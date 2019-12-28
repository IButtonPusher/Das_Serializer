using System;

namespace Das.Serializer.ProtoBuf
{
    public enum ProtoWireTypes
    {
        // ReSharper disable once UnusedMember.Global
        Invalid = -1,
        Varint = 0,
        Int64 = 1,
        LengthDelimited = 2,
        Int32 = 3
    }
}
