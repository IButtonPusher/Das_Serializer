using System;
using System.Threading.Tasks;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public interface IProtoField : INamedField, IEquatable<IProtoField>
    {
        /// <summary>
        ///     Wire Type | Index bit shift left 3
        /// </summary>
        Int32 Header { get; }

        Int32 Index { get; }

        Boolean IsLeafType { get; }

        Boolean IsRepeatedField { get; }

        TypeCode TypeCode { get; }

        ProtoWireTypes WireType { get; }
    }
}
