using System;

namespace Das.Serializer.Objects
{
    public interface IProtoPropertyIterator : IPropertyValueIterator<IProtoProperty>,
        IProtoField
    {
        IProtoPropertyIterator Push();

        IProtoPropertyIterator Pop();

        IProtoPropertyIterator Parent { get; set; }
    }
}
