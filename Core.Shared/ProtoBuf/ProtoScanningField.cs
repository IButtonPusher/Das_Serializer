using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoField : IProtoFieldAccessor
    {
        public ProtoField(String name, Type type, ProtoWireTypes wireType, Int32 fieldIndex,
                          Int32 header, MethodInfo valueGetter, TypeCode typeCode, Boolean isLeaf,
                          Boolean isRepeated, ProtoFieldAction fieldAction, Byte[] headerBytes, MethodInfo? setMethod)
        {
            GetMethod = valueGetter;
            TypeCode = typeCode;
            IsLeafType = isLeaf;
            IsRepeatedField = isRepeated;
            FieldAction = fieldAction;
            HeaderBytes = headerBytes;
            SetMethod = setMethod;
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

        public ProtoFieldAction FieldAction { get; }

        public MethodInfo GetMethod { get; }

        public MethodInfo? SetMethod { get; }

        public Byte[] HeaderBytes { get; }

        public Boolean Equals(IProtoField other)
        {
            if (ReferenceEquals(null, other))
                return false;

            return other.Header == Header && other.Name == Name;
        }

        public bool Equals(ParameterInfo other)
        {
            if (ReferenceEquals(null, other))
                return false;

            return other.ParameterType == Type &&
                   String.Equals(other.Name, Name, StringComparison.OrdinalIgnoreCase);
        }

        public override Int32 GetHashCode()
        {
            return Header;
        }


        public override String ToString()
        {
            return $"{Type.Name} {Name} [{WireType}] protofield";
        }
    }
}