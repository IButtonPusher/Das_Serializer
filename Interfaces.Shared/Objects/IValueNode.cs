using System;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface IValueNode : IStronglyTyped
    {
        Object Value { get; }
    }
}
