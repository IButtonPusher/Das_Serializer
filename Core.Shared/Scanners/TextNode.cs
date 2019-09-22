using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Das.Serializer;

namespace Das.Scanners
{
    internal class TextNode : BaseNode<ITextNode>, ITextNode
    {
        public TextNode(String name, ISerializerSettings settings,
            INodeManipulator nodeManipulator) : base(settings)
        {
            Text = new StringBuilder();
            _nodeManipulator = nodeManipulator;
            Name = name;

            Children = new Dictionary<String, ITextNode>(
                StringComparer.InvariantCultureIgnoreCase);
        }

        private readonly INodeManipulator _nodeManipulator;
        public StringBuilder Text { get; }

        public IDictionary<String, ITextNode> Children { get; }

        public override void Clear()
        {
            foreach (var child in Children.Values)
                child.Clear();

            Children.Clear();

            base.Clear();
            Text.Clear();
        }

        public override Boolean IsEmpty => Name == null && Type == null
                                                        && Children.Count == 0;


        public IEnumerator<ITextNode> GetEnumerator()
        {
            foreach (var node in Children)
            {
                foreach (var grand in node.Value)
                    yield return grand;
            }

            yield return this;
        }

        public override String ToString() => $"Name: {Name} Type: {Type}: Val: {Value} Text: {Text}";
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void AddChild(ITextNode node)
        {
            if (NodeType == NodeTypes.None)
            {
                _nodeManipulator.InferType(this);

                if (!IsUseless(Type))
                    NodeType = _nodeManipulator.GetNodeType(Type,
                        Settings.SerializationDepth);


                if (NodeType == NodeTypes.PropertiesToConstructor)
                    Children.Clear();
            }

            if (NodeType == NodeTypes.Collection)
                Children.Add($"{Children.Count}", node);

            else if (node.Name != null)
            {
                if (Children.ContainsKey(node.Name))
                {
                    //deduce that it's a collection
                    NodeType = NodeTypes.Collection;
                    Children.Add($"{Children.Count}", node);
                }
                else
                    Children.Add(node.Name, node);
            }
            else
            {
                Children.Add($"{Children.Count}", node);
            }
        }


        public void SetText(Object value)
        {
            Text.Clear();
            Text.Append(value);
        }
    }
}