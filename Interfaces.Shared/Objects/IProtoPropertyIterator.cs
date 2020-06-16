using System;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    public interface IProtoPropertyIterator : IPropertyValueIterator<IProtoProperty>,
        IProtoField
    {
        IProtoPropertyIterator Parent { get; set; }

        Boolean MoveNext(ref IProtoPropertyIterator propertyValues);

        IProtoPropertyIterator Pop();

        IProtoPropertyIterator Push();
    }
}