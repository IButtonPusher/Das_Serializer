using System;
using System.Collections;
using System.IO;
using System.Text;
using Das.Serializer;
using Das.Serializer.Objects;
using Das.Serializer.ProtoBuf;
using Das.Serializer.Remunerators;

namespace Das.Printers
{
    internal class ProtoPrinter<TPropertyAttribute> : BinaryPrinter
        where  TPropertyAttribute : Attribute
    {
        private readonly ProtoBufWriter _writer;
        private readonly ITypeManipulator _types;
        private readonly ProtoBufOptions<TPropertyAttribute> _protoSettings;

        public ProtoPrinter(ProtoBufWriter writer, IBinaryState stateProvider,
            ITypeManipulator typeManipulator, ProtoBufOptions<TPropertyAttribute> protoSettings)
            : base(writer, stateProvider)
        {
            _writer = writer;
            _types = typeManipulator;
            _protoSettings = protoSettings;
        }

        public void Print<TObject>(TObject o)
        {
            var typeO = typeof(TObject);

            Object currentVal = o;

            var typeStructure = _types.GetPrintProtoStructure(typeO, _protoSettings, _stateProvider);
            var properyValues = typeStructure.GetPropertyValues(currentVal);
            
            do //nested object loop
            {
                while (properyValues.MoveNext())
                {
                    var pv = properyValues;

                    var pvType = pv.Type;
                    var pvValue = pv.Value;
                    var wireType = pv.WireType;
                    var repeated = properyValues.IsRepeated;

                    //if this is a repeated field, we will print the header once for each value
                    //so don't print it at the property level
                    if (!repeated)
                        _bWriter.WriteInt32(pv.Header);
                    

                    var code = pv.TypeCode;

                    switch (wireType)
                    {
                        case ProtoWireTypes.Varint:
                        case ProtoWireTypes.Int64:
                        case ProtoWireTypes.Int32:
                            if (!Print(pvValue, code))
                                throw new InvalidOperationException();
                            break;
                        case ProtoWireTypes.LengthDelimited:
                            currentVal = pvValue;
                            switch (code)
                            {
                                case TypeCode.String:
                                    WriteString((String)currentVal);
                                    break;
                                case TypeCode.Object:

                                    //byte array - special case
                                    if (pvType == Const.ByteArrayType)
                                    {
                                        var arr = (Byte[]) pvValue;
                                        _bWriter.WriteInt32(arr.Length);
                                        _bWriter.Append(arr);
                                        break;
                                    }

                                    //nested object - have to stack bytes till we know the
                                    //total length
                                    properyValues = properyValues.Push();
                                    if (!repeated)
                                        _bWriter = _writer.Push();

                                    break;
                                case TypeCode.Empty when pvValue is DictionaryEntry _:
                                    break;
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    pv.Dispose();
                }

                properyValues = properyValues.Pop();
                if (properyValues != null)
                    _bWriter = _bWriter.Pop();
                else break;
            } 
            while (true);
        }

        public Stream Stream
        {
            // ReSharper disable once UnusedMember.GlobalQueue
            get => _writer.OutStream;
            set => _writer.OutStream = value;
        }

        public override Boolean PrintNode(INamedValue node)
        {
            using (var print = _printNodePool.GetPrintNode(node))
            {
                switch (node)
                {
                    case IProperty prop:
                        if (!TryPrintHeader(prop))
                            return false;

                        var isLeaf = _typeInferrer.IsLeaf(node.Type, true);

                        return PrintBinaryNode(print, !isLeaf);
                    default:
                        return PrintObject(print);
                }
            }
        }

        protected sealed override void WriteString(String str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var len = bytes.Length;
            
            _bWriter.WriteInt32(len);
            _bWriter.Append(bytes);
        }

        private Boolean TryPrintHeader(INamedField prop, IProtoStructure typeStructure)
        {
            if (!typeStructure.TryGetHeader(prop, out var header))
                return false;
            
            _bWriter.WriteInt32(header);
            return true;
        }

        private Boolean TryPrintHeader(IProperty prop)
        {
            var typeStructure = _types.GetPrintProtoStructure(prop.DeclaringType, _protoSettings,
                _stateProvider);
            return TryPrintHeader(prop, typeStructure);
        }
    }
}
