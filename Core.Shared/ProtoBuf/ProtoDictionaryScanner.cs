using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryScanner : ProtoDictionaryStructure, IProtoStructure
    {
        

        public ProtoDictionaryScanner(Type type, ISerializationDepth depth, ITypeManipulator state, 
            INodePool nodePool, ISerializationCore serializerCore, IProtoFeeder byteFeeder,
            Int32 fieldHeader) 
            : base(type, depth, state, nodePool, serializerCore)
        {
            _byteFeeder = byteFeeder;
            _fieldHeader = fieldHeader;
            _values = new List<Object>();

            _value = (IDictionary)BuildDefault();
        }

        private readonly List<Object> _values;
        private Boolean _isLastValueMeta;
        private readonly IDictionary _value;
        private readonly IProtoFeeder _byteFeeder;
        private readonly Int32 _fieldHeader;

        Boolean IProtoStructure.IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCode,
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

                        peek = _byteFeeder.GetInt32();
                        var len = _byteFeeder.GetInt32();
                        _byteFeeder.Pop();
                        _byteFeeder.Push(len);
                        _isLastValueMeta = true;
                    }

                    return true;
            }

            return true;
        }

        void ITypeStructure.SetPropertyValueUnsafe(String propName, ref Object targetObj, Object propVal)
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
                        //_isLastValueMeta = ValueWireType == ProtoWireTypes.LengthDelimited;
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
