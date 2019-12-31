using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryStructure : ProtoStructure, IProtoStructure
    {
        private readonly ISerializationCore _serializerCore;
        private readonly List<Object> _values;
        private Boolean _isLastValueMeta;
        protected IDictionary _value;

        public ProtoDictionaryStructure(Type type, ISerializationDepth depth, ITypeManipulator state,
            INodePool nodePool, ISerializationCore serializerCore) 
            : base(type, depth, state, nodePool,serializerCore.ObjectInstantiator)
        {
            _serializerCore = serializerCore;
            var gargs = type.GetGenericArguments();
            KeyType = gargs[0];
            ValueType = gargs[1];
            Type = KeyType;
            
            KeyWireType = ProtoStructure.GetWireType(KeyType);
            ValueWireType = ProtoStructure.GetWireType(ValueType);
            
            KeyTypeCode = Type.GetTypeCode(KeyType);
            ValueTypeCode = Type.GetTypeCode(ValueType);

            _values = new List<Object>();

            _value = (IDictionary)BuildDefault();
        }

        public Type KeyType { get; }
        public Type ValueType { get; }

        public TypeCode KeyTypeCode { get; }

        public TypeCode ValueTypeCode { get; }

        public ProtoWireTypes KeyWireType { get; }

        public ProtoWireTypes ValueWireType { get; }

        
        public override Type Type {get;}

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
