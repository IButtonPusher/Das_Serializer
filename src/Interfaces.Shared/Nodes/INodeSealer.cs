using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface INodeSealer<in TNode> : ISettingsUser
        where TNode : INode<TNode>
    {
        void CloseNode(TNode node);

        void Imbue([NotNull] TNode node,
                   String propertyName,
                   Object value);

        void Imbue(TNode childNode);
    }
}
