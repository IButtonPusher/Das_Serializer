using System;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.Properties;

namespace Das.Serializer.ProtoBuf;

public class ProtoField : PropertyActor,
                          IProtoFieldAccessor
{
   public ProtoField(String name,
                     Type type,
                     ProtoWireTypes wireType,
                     Int32 fieldIndex,
                     Int32 header,
                     MethodInfo valueGetter,
                     Boolean isLeaf,
                     Boolean isRepeated,
                     FieldAction fieldAction,
                     Byte[] headerBytes,
                     MethodInfo? setMethod)
      : base(name, type, valueGetter, setMethod,
         fieldAction, fieldIndex)
   {
      IsLeafType = isLeaf;
      IsRepeatedField = isRepeated;
      HeaderBytes = headerBytes;
      WireType = wireType;
      Header = header;
   }

   public ProtoWireTypes WireType { get; }

   public Int32 Header { get; }

   public Boolean IsLeafType { get; }

   public Boolean IsRepeatedField { get; }

      

   public Byte[] HeaderBytes { get; }

   public Boolean Equals(IProtoField? other)
   {
      if (ReferenceEquals(null, other))
         return false;

      return other.Header == Header && other.Name == Name;
   }

   public bool Equals(ParameterInfo? other)
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