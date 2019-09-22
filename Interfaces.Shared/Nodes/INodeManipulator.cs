using System;

namespace Das.Serializer
{
    public interface INodeManipulator : INodeTypeProvider
    {
        void InferType(INode node);

        void EnsureNodeType(INode node, NodeTypes specified);

        void EnsureNodeType(INode node);

        IDynamicType BuildDynamicType(INode node);

        Boolean TryBuildValue(INode node);
    }
}