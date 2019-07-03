using Das.Serializer;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Das.CoreExtensions;
using Das.Serializer.Objects;
using Serializer.Core.Binary;
using Serializer.Core.Printers;
using Serializer.Core.Remunerators;

namespace Das.Printers
{
	internal class BinaryPrinter : PrinterBase<byte>, IDisposable, ISerializationDepth
    {
		public BinaryPrinter(IBinaryWriter writer, ISerializationState stateProvider) 
            : base(stateProvider)
		{
			IsPrintNullProperties = true;
			_bWriter = writer;
            _stateProvider = stateProvider;
            _fallbackFormatter = null;
            IsTextPrinter = false;
		}
        
        bool ISerializationDepth.IsOmitDefaultValues
        {
            get => false;
            set => throw new NotSupportedException();
        }

        SerializationDepth ISerializationDepth.SerializationDepth
        {
            get => Settings.SerializationDepth;
            set => throw new NotSupportedException();
        }

        private IBinaryWriter _bWriter;
        private readonly ISerializationState _stateProvider;
        private BinaryFormatter _fallbackFormatter;
        private BinaryLogger _logger;
        private BinaryLogger Logger => _logger ?? (_logger = new BinaryLogger());
        

        #region public interface

      
        public override Boolean PrintNode(NamedValueNode node)
		{
            var name = node.Name;
            var propType = node.Type;
            var val = node.Value;

            var valType = val?.GetType() ?? propType;
            if (!_isIgnoreCircularDependencies)
                PushStack(name);

            if (node.Name != String.Empty)
                Logger.Debug("*PROP* " + " [" + node.Name + "]");

            try
			{
                var isWrapping = TryWrap(propType, ref val, ref valType);
                var isLeaf = IsLeaf(valType, true);

                Type useType;

                switch (isWrapping)
                {
                    case false when !isLeaf:
                        useType = valType;
                        break;
                    case false when true:
                        useType = propType;
                        break;
                    default:
                        useType = valType;
                        break;
                }    

				var res = _stateProvider.GetNodeType(useType, Settings.SerializationDepth);
				
                var print = new PrintNode(name, val, useType, res, isWrapping);

                if (!isLeaf || isWrapping)
                {
                    Push(print);
                    PrintObject(print);
                    var popping = _bWriter;
                    var wroten = _bWriter.SumLength;
                    _bWriter =_bWriter.Pop();

                    Logger.TabMinus();
                    Logger.Debug("<- POP! TO Index  " + wroten + " " + popping);
                }
                else
                    PrintObject(print);

                return val != null;
			}
			finally
			{
                if (!_isIgnoreCircularDependencies)
                    PopStack();
			}
		}

        private Boolean TryWrap(Type propType, ref object val, ref Type valType)
        {
            var isWrapping = val != null && IsWrapNeeded(propType, valType);

            if (!isWrapping)
                return false;

            if (!TryGetNullableType(propType, out _))
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

		protected override void PrintReferenceType(PrintNode node)			
		{
            Logger.Debug("*REFERENCE* " + node.Value);
			base.PrintReferenceType(node);
		}

        private void Push(PrintNode node)
        {
            Logger.Debug("->PUSH to index " + _bWriter.Length + " " + node);
            
            _bWriter = _bWriter.Push(node);
            Logger.TabPlus();

            if (node.IsWrapping)
               WriteType(node.Value.GetType());
        }

        private Boolean PrintPrimitiveItem(NamedValueNode node)
        {
            var print = new PrintNode(node, NodeTypes.Primitive);
            PrintPrimitive(print);
            return false;
        }

        protected override void PrintPrimitive(PrintNode node)
        {
            while (true)
            {
                var o = node.Value;
                var type = node.Type;

                Logger.Debug("@PRIMITIVE " + type.Name + " = " + o);


                Byte[] bytes;

                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.String:
                        WriteString(o?.ToString());
                        return;
                    case TypeCode.Boolean:
                        bytes = BitConverter.GetBytes((Boolean) o);
                        break;
                    case TypeCode.Char:
                        bytes = BitConverter.GetBytes((Char) o);
                        break;
                    case TypeCode.SByte:
                        _bWriter.WriteInt8((SByte) o);
                        return;
                    case TypeCode.Byte:
                        _bWriter.WriteInt8((Byte) o);
                        return;
                    case TypeCode.Int16:
                        _bWriter.WriteInt16((Int16) o);
                        return;
                    case TypeCode.UInt16:
                        _bWriter.WriteInt16((UInt16) o);
                        return;
                    case TypeCode.Int32:
                        _bWriter.WriteInt32((Int32) o);
                        return;
                    case TypeCode.UInt32:
                        _bWriter.WriteInt32((UInt32) o);
                        return;
                    case TypeCode.Int64:
                        _bWriter.WriteInt64((Int64) o);
                        return;
                    case TypeCode.UInt64:
                        _bWriter.WriteInt64((UInt64) o);
                        return;
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
                        return;
                    default:
                        if (!TryGetNullableType(type, out var primType))
                            throw new NotSupportedException($"Type {type} cannot be printed as a primitive");
                        
                        if (o == null)
                        {
                            Trace.WriteLine("Writing NULL!");
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

                Logger.Debug($"Printing primitive [{o}] length {bytes.Length} {bytes.ToString(',')}");

                _bWriter.Write(bytes);
                break;
            }
        }

        protected override void PrintCollection(PrintNode node)
		{
			var germane = GetGermaneType(node.Type);
           
            var list = node.Value as IEnumerable ?? throw new ArgumentException();
            
            var boom  = ExplodeList(list, germane);

            var isLeaf = IsLeaf(germane, true);

            if (isLeaf)
                PrintSeries(boom, PrintPrimitiveItem);
            else
                PrintSeries(boom, PrintNode);
		}

		protected override void PrintFallback(PrintNode node) 			
		{
			//we have to assume primistring otherwise the node type would have been more specific
			var useType = node.Type;
            var o = node.Value;
			if (o != null && IsUseless(useType))
				useType = o.GetType();			

			if (o == null)
			{
                //specify that the actual object is 0 bytes
                _bWriter.WriteInt32(0);
			}
			else if (IsLeaf(useType, true))
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

                    var length = (Int32)stream.Length;
                    var buff = stream.GetBuffer();
                    _bWriter.Append(buff, length);

                    Logger.Debug("*FALLBACK* " + o + " type: " + node.Type.Name + " length: " +
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
            
            _bWriter.WriteInt8((SByte)index);
		}

		private void WriteType(Type type)
		{
            var typeName = ToClearName(type, false);

            Logger.Debug($"writing type. {typeName} forced: " + 
                (Settings.TypeSpecificity == TypeSpecificity.All));

			WriteString(typeName);
			
		}

		private void WriteString(String str)
		{
			if (str == null)
			{
                //null string indicate such with -1 length
                _bWriter.WriteInt32(-1);
				return;
			}

			var bytes = GetBytes(str);

			var len = bytes.Length;
            Logger.Debug($"[String] {str}  {len} bytes: {bytes.ToString(',')}");
            _bWriter.WriteInt32(len);
			_bWriter.Append(bytes);
		}

		#endregion
	}
}
