using System;

namespace Das.Serializer
{
    public interface INodeSealer<in TNode> : ISettingsUser
        where TNode : INode<TNode>
    {
        void Imbue([NotNull]TNode node, String propertyName, Object value);

        void Imbue(TNode childNode);

        void CloseNode(TNode node);
    }
}