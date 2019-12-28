using System;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoField : IProtoFieldAccessor
    {
        public ProtoField(INamedField field, ProtoWireTypes wireType, Int32 fieldIndex,
            Int32 header, Func<Object, Object> valueGetter, TypeCode typeCode, Boolean isLeaf)
        {
            _valueGetter = valueGetter;
            TypeCode = typeCode;
            IsLeafType = isLeaf;
            Type = field.Type;
            Name = field.Name;
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

        private readonly Func<Object, Object> _valueGetter;

        public Object GetValue(Object @from)
        {
            return _valueGetter(from);
        }
    }
}
