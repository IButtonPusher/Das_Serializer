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
            
            KeyWireType = GetWireType(KeyType);
            ValueWireType = GetWireType(ValueType);
            
            KeyTypeCode = Type.GetTypeCode(KeyType);
            ValueTypeCode = Type.GetTypeCode(ValueType);

            _value = (IDictionary)BuildDefault();
        }

        protected readonly Type KeyType;
        protected readonly Type ValueType;

        protected readonly TypeCode KeyTypeCode;

        protected readonly TypeCode ValueTypeCode;

        protected readonly ProtoWireTypes KeyWireType;

        protected readonly ProtoWireTypes ValueWireType;

        
        public override Type Type {get;}

    }
}
