using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Das.Extensions;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryInfo
    {
        public IProtoField Key { get; }

        public IProtoField Value { get; }

        public IEnumerable<IProtoField> KeyValueFields
        {
            get
            {
                yield return Key;
                yield return Value;
            }
        }

        public Type KeyType { get; }

        public Type ValueType { get; }

        public Type KeyValuePairType { get; }

        public TypeCode KeyTypeCode { get; }

        public TypeCode ValueTypeCode { get; }

        public ProtoWireTypes KeyWireType { get; }

        public ProtoWireTypes ValueWireType { get; }

        public Int32 KeyHeader { get; }

        public Int32 ValueHeader { get; }

        public ProtoDictionaryInfo(Type type, ITypeManipulator types)
        {
            if (type == null || !typeof(IDictionary).IsAssignableFrom(type))
                throw new TypeLoadException(type?.Name);

            var gargs = type.GetGenericArguments();
            KeyType = gargs[0];
            ValueType = gargs[1];

            KeyTypeCode = Type.GetTypeCode(KeyType);
            ValueTypeCode = Type.GetTypeCode(ValueType);

            KeyWireType = ProtoBufSerializer.GetWireType(KeyType);
            ValueWireType = ProtoBufSerializer.GetWireType(ValueType);

            KeyHeader = (Int32) KeyWireType + (1 << 3);
            ValueHeader = (Int32)ValueWireType + (1 << 3);

            KeyValuePairType = typeof(KeyValuePair<,>).
                MakeGenericType(KeyType, ValueType);

            KeyGetter = KeyValuePairType.GetterOrDie(nameof(Key));
            ValueGetter = KeyValuePairType.GetterOrDie(nameof(Value));


            Key = new ProtoField(nameof(Key), KeyType, KeyWireType, 0, KeyHeader,
                null, KeyTypeCode, types.IsLeaf(KeyType, false), false);

            Value= new ProtoField(nameof(Value), ValueType, ValueWireType, 0, ValueHeader,
                null, ValueTypeCode, types.IsLeaf(ValueType, false), false);
        }

        public MethodInfo ValueGetter { get; }

        public MethodInfo KeyGetter { get; }
    }
}
