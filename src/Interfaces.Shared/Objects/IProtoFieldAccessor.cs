using System;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.Properties;

namespace Das.Serializer
{
    public interface IProtoFieldAccessor : IProtoField, IEquatable<ParameterInfo>,
                                           IPropertyActionAware
    {
        /// <summary>
        ///     Wire type | field index
        /// </summary>
        Byte[] HeaderBytes { get; }
    }
}
