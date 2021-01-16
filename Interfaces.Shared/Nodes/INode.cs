using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface INode<TNode> : INode where TNode : INode<TNode>
    {
        [NotNull] new TNode Parent { get; set; }
    }

    public interface INode : IValueNode
    {
        IEnumerable<KeyValuePair<String, AttributeValue>> Attributes { get; }

        Boolean TryGetAttribute(String key,
                                Boolean isRemoveIfFound,
                                out AttributeValue value);

        void AddAttribute(String key,
                          String value,
                          Boolean wasValueInQuotes);

        IDictionary<String, Object?> DynamicProperties { get; }

        Boolean IsEmpty { get; }

        Boolean IsForceNullValue { get; set; }

        [NotNull] String Name { get; }

        NodeTypes NodeType { get; set; }

        [NotNull] INode Parent { get; }

        new Type? Type { get; set; }

        new Object? Value { get; set; }

        void Clear();
    }
}