using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer;

namespace Das.Scanners
{
    internal class BinaryNode : BaseNode<IBinaryNode>, IBinaryNode
    {
        public int BlockSize { get; set; }
        public int BlockStart { get; set; }
        public IList<IBinaryNode> PendingReferences { get; }

        public BinaryNode(String name, ISerializerSettings settings) : base(settings)
        {
            Name = name;

            PendingReferences = new List<IBinaryNode>();
        }

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