using System;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class CoreContext : SerializerCore, ISerializationContext
    {
        public CoreContext(IDynamicFacade dynamicFacade, ISerializerSettings settings) 
            : base(dynamicFacade, settings)
        {
        }

        public abstract INodeProvider NodeProvider { get; }

        public NodeTypes GetNodeType(INode node, SerializationDepth depth)
            => NodeProvider.TypeProvider.GetNodeType(node, depth);

        public NodeTypes GetNodeType(Type type, SerializationDepth depth)
            => NodeProvider.TypeProvider.GetNodeType(type, depth);
    }
}
