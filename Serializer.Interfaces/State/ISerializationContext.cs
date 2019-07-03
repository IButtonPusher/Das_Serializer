using System;

namespace Das.Serializer
{
    public interface ISerializationContext : ISerializationCore,
        INodeTypeProvider
    { }
}
