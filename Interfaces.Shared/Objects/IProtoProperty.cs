using System;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Objects
{
    public interface IProtoProperty :  IProperty
    {
        ProtoWireTypes WireType { get; }
    }
}
