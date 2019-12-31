using System;
using System.Collections;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryStructure : ProtoStructure
    {
        protected IDictionary _value;

        public ProtoDictionaryStructure(Type type, ISerializationDepth depth, ITypeManipulator state,
            INodePool nodePool, ISerializationCore serializerCore) 
            : base(type, depth, state, nodePool,serializerCore.ObjectInstantiator)
        {
            var gargs = type.GetGenericArguments();
            KeyType = gargs[0];
            ValueType = gargs[1];
            Type = KeyType;
            
            KeyWireType = ProtoStructure.GetWireType(KeyType);
            ValueWireType = ProtoStructure.GetWireType(ValueType);
            
            KeyTypeCode = Type.GetTypeCode(KeyType);
            ValueTypeCode = Type.GetTypeCode(ValueType);

            _value = (IDictionary)BuildDefault();
        }

        public Type KeyType { get; }
        public Type ValueType { get; }

        public TypeCode KeyTypeCode { get; }

        public TypeCode ValueTypeCode { get; }

        public ProtoWireTypes KeyWireType { get; }

        public ProtoWireTypes ValueWireType { get; }

        
        public override Type Type {get;}

    }
}
