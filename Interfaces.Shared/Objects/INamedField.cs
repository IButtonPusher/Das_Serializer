using System;

namespace Das.Serializer
{
    public interface INamedField
    {
        String Name { get; }

        Type Type { get; }
    }
}