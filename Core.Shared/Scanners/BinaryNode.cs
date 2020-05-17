using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Serializer.Scanners
{
    internal class BinaryNode : BaseNode<IBinaryNode>, IBinaryNode
    {
        public Int32 BlockSize { get; set; }
        public Int32 BlockStart { get; set; }
        public IList<IBinaryNode> PendingReferences { get; }

        public BinaryNode(String name, ISerializerSettings settings) : base(settings)
        {
            Name = name;

            PendingReferences = new List<IBinaryNode>();
        }

        public override Boolean IsEmpty => BlockSize == 1 && Type != typeof(Byte);

        public override void Clear()
        {
            base.Clear();
            PendingReferences.Clear();
            BlockSize = 0;
            BlockStart = 0;
        }

        public IEnumerator<IBinaryNode> GetEnumerator()
        {
            yield return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}