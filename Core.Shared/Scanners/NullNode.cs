﻿using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Types;

namespace Das.Serializer.Scanners
{
    public class NullNode : ITextNode, IBinaryNode, IEquatable<INode>
    {
        private NullNode()
        {
            Attributes = new InvalidCollection<String, String>();
            DynamicProperties = new InvalidCollection<String, Object>();
            Children = new InvalidCollection<String, ITextNode>();
            PendingReferences = new List<IBinaryNode>();
        }

        

        public static NullNode Instance { get; }

        static NullNode() => Instance = new NullNode();

        INode INode.Parent => Instance;

        ITextNode INode<ITextNode>.Parent
        {
            get => Instance;
            set => throw new InvalidOperationException();
        }

        IBinaryNode INode<IBinaryNode>.Parent
        {
            get => Instance;
            set => throw new InvalidOperationException();
        }

        public Type Type
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public Object Value
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public Boolean IsForceNullValue { get; set; }
        public String Name => String.Empty;
        public Boolean IsEmpty => true;
        public NodeTypes NodeType
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public IDictionary<String, String> Attributes { get; }
        public IDictionary<String, Object> DynamicProperties { get; }
        public void Clear()
        {
            throw new InvalidOperationException();
        }

        IEnumerator<IBinaryNode> IEnumerable<IBinaryNode>.GetEnumerator()
        {
            yield break;
        }

        IEnumerator<ITextNode> IEnumerable<ITextNode>.GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield break;
        }

        public Boolean IsOmitDefaultValues => true;
        public SerializationDepth SerializationDepth => SerializationDepth.None;
        public Boolean IsRespectXmlIgnore => true;
        public void Set(String name, ISerializationDepth depth, INodeManipulator nodeManipulator)
        {
        }

        public String Text => String.Empty;


        public void Append(String str)
        {
        }

        public void Append(Char c)
        {
        }

        public void SetText(Object value)
        {
        }

        public void AddChild(ITextNode node)
        {
            throw new InvalidOperationException();
        }

        public IDictionary<String, ITextNode> Children { get; }
        public Int32 BlockSize { get; set; }
        public Int32 BlockStart { get; set; }
        public IList<IBinaryNode> PendingReferences { get; }

        public Boolean Equals(INode other)
        {
            return ReferenceEquals(Instance, other);
        }

        public override Boolean Equals(Object other)
        {
            return ReferenceEquals(Instance, other);
        }

        public static Boolean operator ==(NullNode nn, INode node)
        {
            return ReferenceEquals(node, nn);
        }

        public static Boolean operator !=(NullNode nn, INode node)
        {
            return !ReferenceEquals(node, nn);
        }

        public override Int32 GetHashCode() => 0;



    }
}