using System;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoField : IProtoFieldAccessor
    {
        public ProtoField(String name, Type type, ProtoWireTypes wireType, Int32 fieldIndex,
            Int32 header, Func<Object, Object> valueGetter, TypeCode typeCode, Boolean isLeaf,
            Boolean isRepeated)
        {
            _valueGetter = valueGetter;
            TypeCode = typeCode;
            IsLeafType = isLeaf;
            IsRepeatedField = isRepeated;
            Type = type;
            Name = name;
            WireType = wireType;
            Index = fieldIndex;
            Header = header;
        }

        public Type Type { get; set; }
        public String Name { get; }
        public ProtoWireTypes WireType { get; }
        public Int32 Header { get; }
        public Int32 Index { get; }
        public TypeCode TypeCode { get; }
        public Boolean IsLeafType { get; }
        public Boolean IsRepeatedField { get; }

        private readonly Func<Object, Object> _valueGetter;

        public Object GetValue(Object @from)
        {
            return _valueGetter(from);
        }

        public override String ToString()
            => $"{Type.Name} {Name} [{WireType}] protofield";
    }
}
