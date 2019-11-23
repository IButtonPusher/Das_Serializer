﻿using System;
using System.Collections.Generic;
using Das.Serializer;
using Serializer.Core;

namespace Das.Scanners
{
    internal abstract class BaseNode<TNode> : TypeCore, INode<TNode>
        where TNode : INode<TNode>
    {
        protected BaseNode(ISerializerSettings settings) : base(settings)
        {
            DynamicProperties = new Dictionary<String, Object>();
            Attributes = new Dictionary<String, String>();
            Name = Const.Empty;
        }

        public Boolean IsForceNullValue { get; set; }

        private String _name;

        public String Name
        {
            get => _name;
            set => _name = value ?? throw new InvalidOperationException();
        }

        
        TNode INode<TNode>.Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public Type Type { get; set; }

        public Object Value { get; set; }

        public IDictionary<String, String> Attributes { get; }
        public IDictionary<String, Object> DynamicProperties { get; }

        public INode Parent => _parent;
        public NodeTypes NodeType { get; set; }

        public virtual Boolean IsEmpty => false;

        private TNode _parent;


        public virtual void Clear()
        {
            Name = Const.Empty;
            IsForceNullValue = false;
            Type = default;
            Value = default;
            Attributes.Clear();
            DynamicProperties.Clear();

            _parent = default;
            NodeType = NodeTypes.None;
        }

        public override String ToString() => $"Name: {Name} Type: {Type}: Val: {Value} ";
    }
}