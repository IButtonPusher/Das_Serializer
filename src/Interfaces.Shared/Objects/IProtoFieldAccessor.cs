using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IProtoFieldAccessor : IProtoField, IEquatable<ParameterInfo>
    {
        ProtoFieldAction FieldAction { get; }

        MethodInfo GetMethod { get; }

        /// <summary>
        ///     Wire type | field index
        /// </summary>
        Byte[] HeaderBytes { get; }

        MethodInfo? SetMethod { get; }

        //Object GetValue(Object from);
    }
}
