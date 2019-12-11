using System;

namespace Das.Serializer
{
    public interface INodeTypeProvider : ISettingsUser
    {
        NodeTypes GetNodeType(INode node, SerializationDepth depth);

        NodeTypes GetNodeType(Type type, SerializationDepth depth);

    }
}