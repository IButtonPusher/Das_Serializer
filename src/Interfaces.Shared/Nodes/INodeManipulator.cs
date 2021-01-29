using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface INodeManipulator
    {
        IPropertyType BuildDynamicType(INode node);

        void EnsureNodeType(INode node,
                            NodeTypes specified);

        void EnsureNodeType(INode node);

        Type? GetChildType(INode parent,
                           INode child);

        void InferType(INode node);

        Boolean TryBuildValue(INode node);
    }
}
