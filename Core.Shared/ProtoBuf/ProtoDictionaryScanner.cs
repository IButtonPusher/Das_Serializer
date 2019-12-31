using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryScanner : IProtoScanStructure
    {
        public ProtoDictionaryScanner(IProtoStructure protoStruct, IProtoFeeder byteFeeder,
            Int32 fieldHeader)
        {
            var gargs = protoStruct.Type.GetGenericArguments();
            KeyType = gargs[0];
            ValueType = gargs[1];

            KeyWireType = ProtoStructure.GetWireType(KeyType);
            ValueWireType = ProtoStructure.GetWireType(ValueType);
            
            KeyTypeCode = Type.GetTypeCode(KeyType);
            ValueTypeCode = Type.GetTypeCode(ValueType);

            _protoStruct = protoStruct;
            _byteFeeder = byteFeeder;
            _fieldHeader = fieldHeader;
            _values = new List<Object>();

            _value = (IDictionary)protoStruct.BuildDefault();
        }

        public void Set(IProtoFeeder byteFeeder, Int32 fieldHeader)
        {
            _byteFeeder = byteFeeder;
            _fieldHeader = fieldHeader;
            _value = (IDictionary)_protoStruct.BuildDefault();
            _values.Clear();
        }

        protected IDictionary _value;
        private readonly List<Object> _values;
        private Boolean _isLastValueMeta;
        private readonly IProtoStructure _protoStruct;
        private IProtoFeeder _byteFeeder;
        private  Int32 _fieldHeader;

        public Type KeyType { get; }
        public Type ValueType { get; }

        public TypeCode KeyTypeCode { get; }

        public TypeCode ValueTypeCode { get; }

        public ProtoWireTypes KeyWireType { get; }

        public ProtoWireTypes ValueWireType { get; }

        public Object BuildDefault()
        {
            return _protoStruct.BuildDefault();
        }

        public Dictionary<Int32, IProtoFieldAccessor> FieldMap => _protoStruct.FieldMap;

        Boolean IProtoScanStructure.IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCode,
            ref Type type)
        {
            switch (_values.Count % 2)
            {
                case 1 when !_isLastValueMeta:
                    wireType = ValueWireType;
                    typeCode = ValueTypeCode;
                    type = ValueType;
                    return true;
                case 1:
                    wireType = ProtoWireTypes.Varint;
                    typeCode = TypeCode.Int32;
                    type = Const.IntType;
                    return true;
                case 0:

                    wireType = KeyWireType;
                    typeCode = KeyTypeCode;
                    type = KeyType;

                    if (!_byteFeeder.HasMoreBytes)
                    {
                        var peek = _byteFeeder.PeekInt32();

                        if (peek != _fieldHeader)
                            return false;

                        var _ = _byteFeeder.GetInt32();
                        var len = _byteFeeder.GetInt32();
                        _byteFeeder.Pop();
                        _byteFeeder.Push(len);
                        _isLastValueMeta = true;
                    }

                    return true;
            }

            return true;
        }

        

        // ReSharper disable once RedundantAssignment
        public Type Type => _protoStruct.Type;

        void ITypeStructureBase.SetPropertyValueUnsafe(String propName, ref Object targetObj, 
            Object propVal)
        {
            targetObj = _value;

            if (_isLastValueMeta)
                _isLastValueMeta = false;
            else
            {
                _values.Add(propVal);

                switch (_values.Count)
                {
                    case 1:
                        _isLastValueMeta = true;
                        break;
                    case 2:
                        _isLastValueMeta = false;
                        _value.Add(_values[0], _values[1]);
                        _values.Clear();
                        break;
                }
            }
        }
    }
}
