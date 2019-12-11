using System;

namespace Das.Serializer
{
    public interface INodeManipulator
    {
        void InferType(INode node);

        Type GetChildType(INode parent, INode child);

        void EnsureNodeType(INode node, NodeTypes specified);

        void EnsureNodeType(INode node);

        IDynamicType BuildDynamicType(INode node);

        Boolean TryBuildValue(INode node);
    }
}