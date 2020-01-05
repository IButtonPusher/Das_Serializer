using System;

namespace Das.Serializer.Objects
{
    public interface IProtoPropertyIterator : IPropertyValueIterator<IProtoProperty>,
        IProtoField
    {
        Boolean MoveNext(ref IProtoPropertyIterator propertyValues);

        IProtoPropertyIterator Push();

        IProtoPropertyIterator Pop();

        IProtoPropertyIterator Parent { get; set; }
    }
}
