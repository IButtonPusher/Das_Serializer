using System;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public interface IProtoField : INamedField
    {
        ProtoWireTypes WireType { get; }

        /// <summary>
        /// Wire Type | Index bit shift left 3
        /// </summary>
        Int32 Header { get; }

        Int32 Index { get; }

        TypeCode TypeCode { get; }

        Boolean IsLeafType { get; }

        Boolean IsRepeated { get; }
    }
}
