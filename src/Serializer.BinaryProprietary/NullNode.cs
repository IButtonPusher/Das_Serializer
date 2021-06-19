using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Das.Serializer.Types;

namespace Das.Serializer
{
    public class NullNode : //ITextNode, 
        IBinaryNode, IEquatable<INode>
    {
        static NullNode()
        {
            Instance = new NullNode();
        }

        private NullNode()
        {
            _attributes = new InvalidCollection<String, AttributeValue>();
            DynamicProperties = new InvalidCollection<String, Object?>();
            //Children = new InvalidCollection<String, ITextNode>();
            PendingReferences = new List<IBinaryNode>();
        }

        IBinaryNode INode<IBinaryNode>.Parent
        {
            get => Instance;
            set => throw new InvalidOperationException();
        }

        IEnumerator<IBinaryNode> IEnumerable<IBinaryNode>.GetEnumerator()
        {
            yield break;
        }

        public Int32 BlockSize { get; set; }

        public Int32 BlockStart { get; set; }

        public IList<IBinaryNode> PendingReferences { get; }

        INode INode.Parent => Instance;

        //ITextNode INode<ITextNode>.Parent
        //{
        //    get => Instance;
        //    set => throw new InvalidOperationException();
        //}

        public Type? Type
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }

        public Object? Value
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

        public IEnumerable<KeyValuePair<String, AttributeValue>> Attributes => _attributes;

        Boolean INode.TryGetAttribute(String key,
                                      Boolean isRemoveIfFound,
                                      out AttributeValue value)
        {
            throw new NotSupportedException();
        }

        void INode.AddAttribute(String key,
                                String value,
                                Boolean wasValueInQuotes)
        {
            throw new NotSupportedException();
        }

        public IDictionary<String, Object?> DynamicProperties { get; }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        //IEnumerator<ITextNode> IEnumerable<ITextNode>.GetEnumerator()
        //{
        //    yield break;
        //}

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield break;
        }

        public Boolean Equals(INode other)
        {
            return ReferenceEquals(Instance, other);
        }

        //public String Text => String.Empty;


        //public void Append(String str)
        //{
        //}

        //public void Append(Char c)
        //{
        //}

        //public void SetText(Object value)
        //{
        //}

        //public void AddChild(ITextNode node)
        //{
        //    throw new InvalidOperationException();
        //}

        //public IDictionary<String, ITextNode> Children { get; }


        public static NullNode Instance { get; }

        public Boolean IsOmitDefaultValues => true;

        public Boolean IsRespectXmlIgnore => true;

        public SerializationDepth SerializationDepth => SerializationDepth.None;

        public override Boolean Equals(Object? other)
        {
            return ReferenceEquals(Instance, other);
        }

        public override Int32 GetHashCode()
        {
            return 0;
        }

        public static Boolean operator ==(NullNode nn,
                                          INode node)
        {
            return ReferenceEquals(node, nn);
        }

        public static Boolean operator !=(NullNode nn,
                                          INode node)
        {
            return !ReferenceEquals(node, nn);
        }

        public void Set(String name,
                        ISerializationDepth depth,
                        INodeManipulator nodeManipulator)
        {
        }

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly InvalidCollection<String, AttributeValue> _attributes;
    }
}
