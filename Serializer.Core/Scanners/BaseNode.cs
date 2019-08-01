using System;
using System.Collections.Generic;
using Das.Serializer;
using Serializer.Core;

namespace Das.Scanners
{
    internal abstract class BaseNode<TNode> : TypeCore, INode<TNode>
        where TNode : INode<TNode>
    {
        public BaseNode(ISerializerSettings settings) : base(settings)
        {
            DynamicProperties = new Dictionary<string, object>();
            Attributes = new Dictionary<string, string>();
        }

        public bool IsForceNullValue { get; set; }
        public String Name { get; set; }

        TNode INode<TNode>.Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public Type Type { get; set; }
        public Object Value { get; set; }
        public IDictionary<String, String> Attributes { get; }
        public IDictionary<string, object> DynamicProperties { get; }

        public INode Parent => _parent;
        public NodeTypes NodeType { get; set; }

        public virtual Boolean IsEmpty => false;

        private TNode _parent;


        public virtual void Clear()
        {
            Name = default;
            IsForceNullValue = false;
            Type = default;
            Value = default;
            Attributes.Clear();
            DynamicProperties.Clear();

            _parent = default;
            NodeType = NodeTypes.None;
        }

        public override string ToString() => $"Name: {Name} Type: {Type}: Val: {Value} ";
    }
}