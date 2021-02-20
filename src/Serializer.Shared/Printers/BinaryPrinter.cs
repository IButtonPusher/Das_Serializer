using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Das.Serializer;
using Das.Serializer.Remunerators;

namespace Das.Printers
{
    public class BinaryPrinter : PrinterBase, 
                                 IDisposable, 
                                 ISerializationDepth
    {
        public BinaryPrinter(IBinaryWriter writer,
                             //IBinaryState stateProvider,
                             ISerializerSettings settings,
                             ITypeInferrer typeInferrer,
                             INodeTypeProvider nodeTypes,
                             IObjectManipulator objectManipulator)
            : base(settings, typeInferrer, nodeTypes, objectManipulator)
        {
            _fallbackFormatter = new BinaryFormatter();
            IsPrintNullProperties = true;
            _bWriter = writer;
            //_stateProvider = stateProvider;

            IsTextPrinter = false;
        }

        public void Dispose()
        {
            _bWriter.Flush();
        }

        Boolean ISerializationDepth.IsOmitDefaultValues => false;

        SerializationDepth ISerializationDepth.SerializationDepth
            => Settings.SerializationDepth;

        public override Boolean IsRespectXmlIgnore => false;

        #if !PARTIALTRUST


        public static unsafe Byte[] GetBytes(String str)
        {
            var len = str.Length * 2;
            var bytes = new Byte[len];
            fixed (void* ptr = str)
            {
                System.Runtime.InteropServices.Marshal.Copy(new IntPtr(ptr), bytes, 0, len);
            }

            return bytes;
        }

#else

        public static Byte[] GetBytes(String str)
        {
        return System.Text.Encoding.UTF8.GetBytes(str);
        }

#endif

        public static Byte[] GetBytes(Decimal dec)
        {
            var bits = Decimal.GetBits(dec);
            var bytes = new List<Byte>();

            foreach (var i in bits)
            {
                bytes.AddRange(BitConverter.GetBytes(i));
            }


            return bytes.ToArray();
        }


        //public override void PrintNode(INamedValue node)
        //{
        //    var name = node.Name;
        //    var propType = node.Type;
        //    var val = node.Value;

        //    var valType = val?.GetType() ?? propType;
        //    if (!_isIgnoreCircularDependencies)
        //        PushStack(name);

        //    try
        //    {
        //        var isWrapping = TryWrap(propType!, ref val, ref valType!);
        //        var isLeaf = _typeInferrer.IsLeaf(valType, true);

        //        switch (isWrapping)
        //        {
        //            case false when !isLeaf:
        //                node.Type = valType;
        //                break;
        //            case false:
        //                //  node.Type = propType;
        //                break;
        //            default:
        //                node.Type = valType;
        //                break;
        //        }

        //        using (var print = _printNodePool.GetPrintNode(node))
        //        {
        //            print.IsWrapping = isWrapping;

        //            PrintBinaryNode(print, !isLeaf || isWrapping);
        //        }
        //    }
        //    finally
        //    {
        //        if (!_isIgnoreCircularDependencies)
        //            PopStack();
        //    }
        //}

        public override void PrintNode(String name,
                                       Type? propType,
                                       Object? val)
        {
            var valType = val?.GetType() ?? propType;
            if (!_isIgnoreCircularDependencies)
                PushStack(name);

            try
            {
                var isWrapping = TryWrap(propType!, ref val, ref valType!);
                var isLeaf = _typeInferrer.IsLeaf(valType, true);

                switch (isWrapping)
                {
                    case false when !isLeaf:
                        propType = valType;
                        break;

                    case false:
                        break;

                    default:
                        propType = valType;
                        break;
                }

                //using (var named = _printNodePool.GetNamedValue(name, val, propType))
                //using (var print = _printNodePool.GetPrintNode(named))
                {
                    var nodeType = _nodeTypes.GetNodeType(propType);
                    PrintBinaryNode(val, propType!, nodeType, isWrapping, !isLeaf || isWrapping);
                }
            }
            finally
            {
                if (!_isIgnoreCircularDependencies)
                    PopStack();
            }
        }

        [MethodImpl(256)]
        protected Boolean Print(Object? o,
                                TypeCode code)
        {
            Byte[] bytes;

            switch (code)
            {
                case TypeCode.String:
                    WriteString(o?.ToString());
                    return true;
                case TypeCode.Boolean:
                    bytes = BitConverter.GetBytes((Boolean) o!);
                    break;
                case TypeCode.Char:
                    bytes = BitConverter.GetBytes((Char) o!);
                    break;
                case TypeCode.SByte:
                    _bWriter.WriteInt8((SByte) o!);
                    return true;
                case TypeCode.Byte:
                    _bWriter.WriteInt8((Byte) o!);
                    return true;
                case TypeCode.Int16:
                    _bWriter.WriteInt16((Int16) o!);
                    return true;
                case TypeCode.UInt16:
                    _bWriter.WriteInt16((UInt16) o!);
                    return true;
                case TypeCode.Int32:
                    _bWriter.WriteInt32((Int32) o!);
                    return true;
                case TypeCode.UInt32:
                    _bWriter.WriteInt32((UInt32) o!);
                    return true;
                case TypeCode.Int64:
                    _bWriter.WriteInt64((Int64) o!);
                    return true;
                case TypeCode.UInt64:
                    _bWriter.WriteInt64((UInt64) o!);
                    return true;
                case TypeCode.Single:
                    bytes = BitConverter.GetBytes((Single) o!);
                    break;
                case TypeCode.Double:
                    bytes = BitConverter.GetBytes((Double) o!);
                    break;
                case TypeCode.Decimal:
                    bytes = GetBytes((Decimal) o!);
                    break;
                case TypeCode.DateTime:
                    _bWriter.WriteInt64(((DateTime) o!).Ticks);
                    return true;
                default:
                    return false;
            }

            _bWriter.Write(bytes);
            return true;
        }

        //protected Boolean PrintBinaryNode(IPrintNode print,
        //                                  Boolean isPush)
        //{
        //    if (isPush)
        //    {
        //        Push(print);
        //        PrintObject(print);

        //        _bWriter = _bWriter.Pop();
        //    }
        //    else
        //        return PrintObject(print);

        //    return print.Value != null;
        //}

        protected Boolean PrintBinaryNode(Object? nodeValue,
                                          Type propType,
                                          NodeTypes nodeType,
                                          Boolean isWrapping,
                                          Boolean isPush)
        {
            if (isPush)
            {
                Push(nodeValue, nodeType, isWrapping);
                PrintObject(nodeValue, propType, nodeType);

                _bWriter = _bWriter.Pop();
            }
            else
                PrintObject(nodeValue, propType, nodeType);

            return nodeValue != null;
        }


        protected override void PrintCircularDependency(Int32 index)
        {
            if (index > 255)
                throw new FieldAccessException();

            _bWriter.WriteInt8((SByte) index);
        }

        //protected override void PrintCollection(IPrintNode node)
        //{
        //    var germane = _typeInferrer.GetGermaneType(node.Type!);

        //    switch (node.Value)
        //    {
        //        case null:
        //            _bWriter.WriteInt8(0);
        //            break;
        //        case IEnumerable list:
        //            var boom = ExplodeList(list, germane);

        //            var isLeaf = _typeInferrer.IsLeaf(germane, true);

        //            if (isLeaf)
        //                PrintSeries(boom, PrintPrimitiveItem);
        //            else
        //                PrintSeries(boom, PrintNode);
        //            break;
        //        default:
        //            throw new InvalidOperationException(
        //                $"{node.Value} is not valid to be printed as a collection");
        //    }
        //}

        protected override void PrintCollection(Object? value,
                                                Type valType,
                                                Boolean knownEmpty)
        {
            var germane = _typeInferrer.GetGermaneType(valType);

            switch (value)
            {
                case null:
                    _bWriter.WriteInt8(0);
                    break;
                case IEnumerable list:
                    //var boom = ExplodeList(list, germane);
                    var boom = ExplodeIterator(list, germane);

                    var isLeaf = _typeInferrer.IsLeaf(germane, true);

                    if (isLeaf)
                        PrintSeries(boom, PrintPrimitiveItem);
                    else
                        PrintSeries(boom, PrintCollectionObject);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{value} is not valid to be printed as a collection");
            }
        }

        //protected override void PrintFallback(IPrintNode node)
        //{
        //    //we have to assume primistring otherwise the node type would have been more specific
        //    var useType = node.Type;
        //    var o = node.Value;
        //    if (o != null && _typeInferrer.IsUseless(useType))
        //        useType = o.GetType();

        //    if (o == null)
        //        //specify that the actual object is 0 bytes
        //        _bWriter.WriteInt32(0);
        //    else if (_typeInferrer.IsLeaf(useType!, true))
        //    {
        //        node.Type = useType;
        //        PrintPrimitive(node);
        //    }
        //    else
        //        using (var stream = new MemoryStream())
        //        {
        //            //_fallbackFormatter = new BinaryFormatter();
        //            _fallbackFormatter.Serialize(stream, o);

        //            var length = (Int32) stream.Length;
        //            var buff = stream.GetBuffer();
        //            _bWriter.Append(buff, length);
        //        }
        //}

        protected override void PrintFallback(Object? o,
                                              Type useType)
        {
            if (o != null && _typeInferrer.IsUseless(useType))
                useType = o.GetType();

            if (o == null)
                //specify that the actual object is 0 bytes
                _bWriter.WriteInt32(0);
            else if (_typeInferrer.IsLeaf(useType!, true))
            {
                //node.Type = useType;
                PrintPrimitive(o, useType);
            }
            else
                using (var stream = new MemoryStream())
                {
                    //_fallbackFormatter = new BinaryFormatter();
                    _fallbackFormatter.Serialize(stream, o);

                    var length = (Int32) stream.Length;
                    var buff = stream.GetBuffer();
                    _bWriter.Append(buff, length);
                }
        }

        //protected override void PrintPrimitive(IPrintNode node)
        //{
        //    while (true)
        //    {
        //        var o = node.Value;
        //        var type = node.Type;

        //        var code = Type.GetTypeCode(type);

        //        if (Print(o, code))
        //            return;

        //        if (!_typeInferrer.TryGetNullableType(type!, out var primType))
        //            throw new NotSupportedException($"Type {type} cannot be printed as a primitive");

        //        if (o == null)
        //            //null
        //            _bWriter.WriteInt8(0);
        //        else
        //        {
        //            //flag that there is a value
        //            _bWriter.WriteInt8(1);
        //            node.Type = primType;
        //            continue;
        //        }

        //        return;
        //    }
        //}

        protected override void PrintPrimitive(Object? o,
                                               Type type)
        {
            while (true)
            {

                var code = Type.GetTypeCode(type);

                if (Print(o, code))
                    return;

                if (!_typeInferrer.TryGetNullableType(type!, out var primType))
                    throw new NotSupportedException($"Type {type} cannot be printed as a primitive");

                if (o == null)
                    //null
                    _bWriter.WriteInt8(0);
                else
                {
                    //flag that there is a value
                    _bWriter.WriteInt8(1);
                    type = primType;
                    continue;
                }

                return;
            }
        }

       

        protected virtual void WriteString(String? str)
        {
            if (str == null)
            {
                //null string indicate such with -1 length
                _bWriter.WriteInt32(-1);
                return;
            }

            var bytes = GetBytes(str);

            var len = bytes.Length;
            _bWriter.WriteInt32(len);
            _bWriter.Append(bytes);
        }

        //private void PrintPrimitiveItem(NamedValueNode node)
        //{
        //    using (var print = _printNodePool.GetPrintNode(node))
        //    {
        //        PrintPrimitive(print);
        //    }
        //}

        private void PrintPrimitiveItem(Object? o,
                                        Type propType,
                                        Int32 index)
        {
            //using (var print = _printNodePool.GetPrintNode(node))
            {
                PrintPrimitive(o, propType);
            }
        }

        private void Push(Object? nodeValue,
                          NodeTypes nodeType,
                          Boolean isWrapping)
        {
            _bWriter = _bWriter.Push(nodeType, isWrapping);

            if (isWrapping)
                WriteType(nodeValue!.GetType());
        }

        //private void Push(IPrintNode node)
        //{
        //    _bWriter = _bWriter.Push(node);

        //    if (node.IsWrapping)
        //        WriteType(node.Value!.GetType());
        //}

        private Boolean TryWrap(Type propType,
                                ref Object? val,
                                ref Type valType)
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

        private void WriteType(Type type)
        {
            var typeName = _typeInferrer.ToClearName(type);

            WriteString(typeName);
        }

        private readonly BinaryFormatter _fallbackFormatter;
        //protected readonly ISerializationState _stateProvider;

        protected IBinaryWriter _bWriter;
    }
}
