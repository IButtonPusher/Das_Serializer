using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface INode<TNode> : INode where TNode : INode<TNode>
    {
        new TNode Parent { get; set; }
    }

    public interface INode
    {
        INode Parent { get; }

        Type Type { get; set; }

        Object Value { get; set; }

        Boolean IsForceNullValue { get; set; }

        String Name { get; }

        Boolean IsEmpty { get; }

        NodeTypes NodeType { get; set; }

        IDictionary<String, String> Attributes { get; }

        IDictionary<String, Object> DynamicProperties { get; }

        void Clear();
    }
}
