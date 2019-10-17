using System;
using System.Collections.Generic;
using Das.Serializer.Annotations;

namespace Das.Serializer
{
    public interface INode<TNode> : INode where TNode : INode<TNode>
    {
        [NotNull]
        new TNode Parent { get; set; }
    }

    public interface INode
    {
        [NotNull]
        INode Parent { get; }

        Type Type { get; set; }

        Object Value { get; set; }

        Boolean IsForceNullValue { get; set; }

        [NotNull]
        String Name { get; }

        Boolean IsEmpty { get; }

        NodeTypes NodeType { get; set; }

        IDictionary<String, String> Attributes { get; }

        IDictionary<String, Object> DynamicProperties { get; }

        void Clear();
    }
}