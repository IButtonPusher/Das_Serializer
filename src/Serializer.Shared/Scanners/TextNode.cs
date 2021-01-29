using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class TextNode : BaseNode<ITextNode>, ITextNode
    {
        public TextNode(String name,
                        ISerializerSettings settings,
                        INodeManipulator nodeManipulator,
                        INodeTypeProvider nodeTypeProvider,
                        ISerializationDepth depth)
            : base(settings)
        {
            _text = new StringBuilder();
            _nodeManipulator = nodeManipulator;
            _nodeTypeProvider = nodeTypeProvider;
            _depth = depth;
            Name = name;

            Children = new Dictionary<String, ITextNode>(
                StringComparer.InvariantCultureIgnoreCase);
        }

        public void Set(String name,
                        ISerializationDepth depth,
                        INodeManipulator nodeManipulator)
        {
            Name = name;
            _depth = depth;
            _nodeManipulator = nodeManipulator;
        }

        public String Text => _text.ToString();

        [MethodImpl(256)]
        public void Append(String str)
        {
            _text.Append(str);
        }

        [MethodImpl(256)]
        public void Append(Char c)
        {
            _text.Append(c);
        }


        public IDictionary<String, ITextNode> Children { get; }

        public override void Clear()
        {
            foreach (var child in Children.Values)
            {
                child.Clear();
            }

            Children.Clear();
            _nodeManipulator = default;

            base.Clear();
            _text.Clear();
        }

        public override Boolean IsEmpty => Name == Const.Empty && Type == null
                                                               && Children.Count == 0;


        public IEnumerator<ITextNode> GetEnumerator()
        {
            foreach (var node in Children)
            foreach (var grand in node.Value)
            {
                yield return grand;
            }

            yield return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddChild(ITextNode node)
        {
            if (NodeType == NodeTypes.None)
            {
                if (Type == null)
                    _nodeManipulator!.InferType(this);

                if (!IsUseless(Type))
                {
                    NodeType = _nodeTypeProvider.GetNodeType(Type,
                        Settings.SerializationDepth);

                    if (node.Type == null)
                    {
                        var someType = _nodeManipulator!.GetChildType(this, node);
                        if (!IsUseless(someType))
                            node.Type = someType;
                    }
                }

                if (NodeType == NodeTypes.PropertiesToConstructor)
                    Children.Clear();
            }

            if (NodeType == NodeTypes.Collection)
                Children.Add($"{Children.Count}", node);

            else if (node.Name != Const.Empty)
                Children.Add(node.Name, node);
            else
                Children.Add($"{Children.Count}", node);
        }


        public void SetText(Object value)
        {
            _text.Clear();
            _text.Append(value);
        }

        public Boolean IsOmitDefaultValues => _depth.IsOmitDefaultValues;

        public SerializationDepth SerializationDepth => _depth.SerializationDepth;

        public Boolean IsRespectXmlIgnore => _depth.IsRespectXmlIgnore;

        public override String ToString()
        {
            return $"Name: {Name} Type: {Type}: Val: {Value} Text: {Text}";
        }

        private readonly INodeTypeProvider _nodeTypeProvider;

        private readonly StringBuilder _text;
        private ISerializationDepth _depth;


        private INodeManipulator? _nodeManipulator;
    }
}
