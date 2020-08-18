using System;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoCollectionItem : IProtoField
    {
        public ProtoCollectionItem(Type collectionType, ITypeManipulator types, Int32 fieldIndex)
        {
            var germane = types.GetGermaneType(collectionType);
            var wire = ProtoBufSerializer.GetWireType(germane);
            var isValidLengthDelim = wire == ProtoWireTypes.LengthDelimited
                                     && germane != Const.StrType 
                                     && germane != Const.ByteArrayType;
            IsRepeatedField = isValidLengthDelim && types.IsCollection(germane);

            Name = String.Empty;
            Type = germane;
            WireType = wire;
            Header = (Int32)wire + (fieldIndex << 3);
            Index = 1;
            IsLeafType = types.IsLeaf(germane, false);
            TypeCode = Type.GetTypeCode(germane);

        }

        public Type Type { get; set; }

        public String Name { get; }

        public Boolean Equals(IProtoField other)
        {
            throw new NotImplementedException();
        }

        public ProtoWireTypes WireType { get; }

        public Int32 Header { get; }

        public Int32 Index { get; }

        public TypeCode TypeCode { get; }

        public Boolean IsLeafType { get; }

        public Boolean IsRepeatedField { get; }
    }
}
