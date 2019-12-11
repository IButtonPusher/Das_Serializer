using System;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface INamedField : IEquatable<INamedField>, IStronglyTyped
    {
        String Name { get; }
    }
}