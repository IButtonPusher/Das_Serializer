using System;
using System.Reflection;

namespace Das.Serializer
{
    public interface IProtoFieldAccessor : IProtoField
    {
        ProtoFieldAction FieldAction { get; }

        MethodInfo GetMethod { get; }

        MethodInfo? SetMethod { get; }

        /// <summary>
        /// Wire type | field index
        /// </summary>
        Byte[] HeaderBytes { get; }

        //Object GetValue(Object from);
    }
}
