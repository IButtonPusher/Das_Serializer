using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryInfo
    {
        public ProtoDictionaryInfo(Type type, ITypeManipulator types,
                                   IProtoProvider protoProvider)
        {
            if (type == null || !typeof(IDictionary).IsAssignableFrom(type))
                throw new TypeLoadException(type?.Name);

            var gargs = type.GetGenericArguments();
            KeyType = gargs[0];
            ValueType = gargs[1];

            KeyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(KeyType, ValueType);

            var keyProp = KeyValuePairType.GetProperty(nameof(KeyValuePair<Object, Object>.Key));
            var valProp = KeyValuePairType.GetProperty(nameof(KeyValuePair<Object, Object>.Value));

            protoProvider.TryGetProtoField(keyProp, false, out var keyField);
            protoProvider.TryGetProtoField(valProp, false, out var valField);

            Key = keyField;
            Value = valField;

            KeyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(KeyType, ValueType);

            //var gargs = type.GetGenericArguments();
            //KeyType = gargs[0];
            //ValueType = gargs[1];

            //KeyTypeCode = Type.GetTypeCode(KeyType);
            //ValueTypeCode = Type.GetTypeCode(ValueType);

            //KeyWireType = ProtoBufSerializer.GetWireType(KeyType);
            //ValueWireType = ProtoBufSerializer.GetWireType(ValueType);

            //KeyHeader = (Int32) KeyWireType + (1 << 3);
            //ValueHeader = (Int32)ValueWireType + (1 << 3);


            //KeyGetter = KeyValuePairType.GetterOrDie(nameof(Key), out _);
            //ValueGetter = KeyValuePairType.GetterOrDie(nameof(Value), out _);

            //var keyAction = protoProvider.GetProtoFieldAction(KeyType);
            //var valueAction = protoProvider.GetProtoFieldAction(KeyType);

            //Key = new ProtoField(nameof(Key), KeyType, KeyWireType, 0, KeyHeader,
            //    null, KeyTypeCode, types.IsLeaf(KeyType, false), false, keyAction);

            //Value= new ProtoField(nameof(Value), ValueType, ValueWireType, 0, ValueHeader,
            //    null, ValueTypeCode, types.IsLeaf(ValueType, false), false, valueAction);
        }

        public IProtoFieldAccessor Key { get; }

        public MethodInfo KeyGetter => Key.GetMethod;

        public Int32 KeyHeader => Key.Header;

        public Type KeyType { get; }

        public TypeCode KeyTypeCode => Key.TypeCode;

        public IEnumerable<IProtoField> KeyValueFields
        {
            get
            {
                yield return Key;
                yield return Value;
            }
        }

        public Type KeyValuePairType { get; }

        public ProtoWireTypes KeyWireType => Key.WireType;

        public IProtoFieldAccessor Value { get; }

        public MethodInfo ValueGetter => Value.GetMethod;

        public Int32 ValueHeader => Value.Header;

        public Type ValueType { get; }

        public TypeCode ValueTypeCode => Value.TypeCode;

        public ProtoWireTypes ValueWireType => Value.WireType;
    }
}