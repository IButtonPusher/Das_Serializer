using Das.Serializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using Das.CoreExtensions;
using Das.Serializer.Objects;
using Serializer.Core.Remunerators;

namespace Das.Printers
{
    internal class BinaryPrinter : PrinterBase<Byte>, IDisposable, ISerializationDepth
    {
        public BinaryPrinter(IBinaryWriter writer, IBinaryState stateProvider)
            : base(stateProvider)
        {
            IsPrintNullProperties = true;
            _bWriter = writer;
            _stateProvider = stateProvider;
            _fallbackFormatter = null;
            IsTextPrinter = false;
            _logger = stateProvider.Logger;
        }

        Boolean ISerializationDepth.IsOmitDefaultValues => false;

        SerializationDepth ISerializationDepth.SerializationDepth 
            => Settings.SerializationDepth;

        public override Boolean IsRespectXmlIgnore => false;

        protected IBinaryWriter _bWriter;
        protected readonly ISerializationState _stateProvider;
        private BinaryFormatter _fallbackFormatter;
        private BinaryLogger _logger;


        #region public interface

        public override Boolean PrintNode(INamedValue node)
        {
            var name = node.Name;
            var propType = node.Type;
            var val = node.Value;

            var valType = val?.GetType() ?? propType;
            if (!_isIgnoreCircularDependencies)
                PushStack(name);

            try
            {
                var isWrapping = TryWrap(propType, ref val, ref valType);
                var isLeaf = _typeInferrer.IsLeaf(valType, true);

                switch (isWrapping)
                {
                    case false when !isLeaf:
                        node.Type = valType;
                        break;
                    case false:
                        //  node.Type = propType;
                        break;
                    default:
                        node.Type = valType;
                        break;
                }
                
                using (var print = _printNodePool.GetPrintNode(node))
                {
                    print.IsWrapping = isWrapping;

                    return PrintBinaryNode(print, !isLeaf || isWrapping);
                }
            }
            finally
            {
                if (!_isIgnoreCircularDependencies)
                    PopStack();
            }
        }

        protected Boolean PrintBinaryNode(IPrintNode print, Boolean isPush)
        { 
            if (isPush)
            {
                Push(print);
                PrintObject(print);
                
                _bWriter = _bWriter.Pop();
            }
            else
                return PrintObject(print);

            return print.Value != null;
        }

        private Boolean TryWrap(Type propType, ref Object val, ref Type valType)
        {
            var isWrapping = val != null && IsWrapNeeded(propType, valType);

            if (!isWrapping)
                return false;

            if (!_typeInferrer.TryGetNullableType(propType, out _))
                return true;

            val = Activator.CreateInstance(propType, val);

            valType = propType;

            return false;
        }

        public void Dispose()
        {
            _bWriter.Flush();
        }

        #endregion

        #region private implementation primary

        private void Push(IPrintNode node)
        {
            _bWriter = _bWriter.Push(node);
            _logger.TabPlus();

            if (node.IsWrapping)
                WriteType(node.Value.GetType());
        }

        private Boolean PrintPrimitiveItem(NamedValueNode node)
        {
            using (var print = _printNodePool.GetPrintNode(node))
                PrintPrimitive(print);

            return false;
        }

        [MethodImpl(256)]
        protected Boolean Print(Object o, TypeCode code)
        {
            Byte[] bytes;

            switch (code)
            {
                case TypeCode.String:
                    WriteString(o?.ToString());
                    return true;
                case TypeCode.Boolean:
                    bytes = BitConverter.GetBytes((Boolean) o);
                    break;
                case TypeCode.Char:
                    bytes = BitConverter.GetBytes((Char) o);
                    break;
                case TypeCode.SByte:
                    _bWriter.WriteInt8((SByte) o);
                    return true;
                case TypeCode.Byte:
                    _bWriter.WriteInt8((Byte) o);
                    return true;
                case TypeCode.Int16:
                    _bWriter.WriteInt16((Int16) o);
                    return true;
                case TypeCode.UInt16:
                    _bWriter.WriteInt16((UInt16) o);
                    return true;
                case TypeCode.Int32:
                    _bWriter.WriteInt32((Int32) o);
                    return true;
                case TypeCode.UInt32:
                    _bWriter.WriteInt32((UInt32) o);
                    return true;
                case TypeCode.Int64:
                    _bWriter.WriteInt64((Int64) o);
                    return true;
                case TypeCode.UInt64:
                    _bWriter.WriteInt64((UInt64) o);
                    return true;
                case TypeCode.Single:
                    bytes = BitConverter.GetBytes((Single) o);
                    break;
                case TypeCode.Double:
                    bytes = BitConverter.GetBytes((Double) o);
                    break;
                case TypeCode.Decimal:
                    bytes = GetBytes((Decimal) o);
                    break;
                case TypeCode.DateTime:
                    _bWriter.WriteInt64(((DateTime) o).Ticks);
                    return true;
                default:
                    return false;
            }

            _bWriter.Write(bytes);
            return true;
        }

        protected override void PrintPrimitive(IPrintNode node)
        {
            while (true)
            {
                var o = node.Value;
                var type = node.Type;

                Byte[] bytes;
                var code = Type.GetTypeCode(type);

                if (Print(o, code))
                    return;

                if (!_typeInferrer.TryGetNullableType(type, out var primType))
                    throw new NotSupportedException($"Type {type} cannot be printed as a primitive");

                if (o == null)
                {
                    //null
                    _bWriter.WriteInt8(0);
                }
                else
                {
                    //flag that there is a value
                    _bWriter.WriteInt8(1);
                    node.Type = primType;
                    continue;
                }

                return;
            }
        }

        protected override void PrintCollection(IPrintNode node)
        {
            var germane = _typeInferrer.GetGermaneType(node.Type);

            var list = node.Value as IEnumerable ?? throw new ArgumentException();

            var boom = ExplodeList(list, germane);

            var isLeaf = _typeInferrer.IsLeaf(germane, true);

            if (isLeaf)
                PrintSeries(boom, PrintPrimitiveItem);
            else
                PrintSeries(boom, PrintNode);
        }


        public static unsafe Byte[] GetBytes(String str)
        {
            var len = str.Length * 2;
            var bytes = new Byte[len];
            fixed (void* ptr = str)
            {
                Marshal.Copy(new IntPtr(ptr), bytes, 0, len);
            }

            return bytes;
        }

        public static Byte[] GetBytes(Decimal dec)
        {
            var bits = Decimal.GetBits(dec);
            var bytes = new List<Byte>();

            foreach (var i in bits)
                bytes.AddRange(BitConverter.GetBytes(i));


            return bytes.ToArray();
        }

        protected override void PrintFallback(IPrintNode node)
        {
            //we have to assume primistring otherwise the node type would have been more specific
            var useType = node.Type;
            var o = node.Value;
            if (o != null && _typeInferrer.IsUseless(useType))
                useType = o.GetType();

            if (o == null)
            {
                //specify that the actual object is 0 bytes
                _bWriter.WriteInt32(0);
            }
            else if (_typeInferrer.IsLeaf(useType, true))
            {
                node.Type = useType;
                PrintPrimitive(node);
            }
            else
            {
                using (var stream = new MemoryStream())
                {
                    _fallbackFormatter = new BinaryFormatter();
                    _fallbackFormatter.Serialize(stream, o);

                    var length = (Int32) stream.Length;
                    var buff = stream.GetBuffer();
                    _bWriter.Append(buff, length);

                    _logger.Debug("*FALLBACK* " + o + " type: " + node.Type.Name + " length: " +
                                 length + " data: " + buff.Take(0, length).ToString(','));
                }
            }
        }

        #endregion

        #region private implementation helpers

        protected override void PrintCircularDependency(Int32 index)
        {
            if (index > 255)
                throw new FieldAccessException();

            _bWriter.WriteInt8((SByte) index);
        }

        private void WriteType(Type type)
        {
            var typeName = _typeInferrer.ToClearName(type, false);

            _logger.Debug($"writing type. {typeName} forced: " +
                         (Settings.TypeSpecificity == TypeSpecificity.All));

            WriteString(typeName);
        }

        protected virtual void WriteString(String str)
        {
            if (str == null)
            {
                //null string indicate such with -1 length
                _bWriter.WriteInt32(-1);
                return;
            }

            var bytes = GetBytes(str);

            var len = bytes.Length;
            _logger.Debug($"[String] {str}  {len} bytes: {bytes.ToString(',')}");
            _bWriter.WriteInt32(len);
            _bWriter.Append(bytes);
        }

        #endregion
    }
}