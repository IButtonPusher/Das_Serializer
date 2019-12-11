using System;

namespace Das.Serializer.Objects
{
    public interface IProperty : INamedValue
    {
        Type DeclaringType { get; }
    }
}
