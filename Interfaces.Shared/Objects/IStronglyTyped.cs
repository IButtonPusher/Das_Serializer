using System;

namespace Das.Serializer.Objects
{
    public interface IStronglyTyped
    {
        Type Type { get; set; }
    }
}
