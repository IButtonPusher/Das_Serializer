using System;
using System.Threading.Tasks;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Objects
{
    public interface IProtoProperty : IProperty
    {
        ProtoWireTypes WireType { get; }
    }
}
