using System;

namespace Das.Serializer.Objects
{
    public interface IProtoPropertyIterator : IPropertyValueIterator<IProtoProperty>,
        IProtoField
    {
        void Push();

        Boolean Pop();

        Boolean IsCollection { get; }
    }
}
