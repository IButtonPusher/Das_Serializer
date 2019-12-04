using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Das.Remunerators;
using Das.Serializer;
using Das.Serializer.Remunerators;
using Serializer.Core.Printers;

namespace Serializer.Core.Remunerators
{
    /// <summary>
    /// Writes to a temporary collection that is eventually merged back into the main stream (or parent deferred)
    /// along with the length of the data for fixed length deserialization
    /// </summary>
    public unsafe class DeferredBinaryWriter : BinaryWriterWrapper, IBinaryWriter
    {
        public DeferredBinaryWriter(PrintNode node, IBinaryWriter parent)
            : base(parent)
        {
            //ParentIndex = index;
            _backingList = new ByteBuilder();
            _node = node;
            _parent = parent;

            _isWrapPossible = node.NodeType != NodeTypes.Primitive || node.IsWrapping;

            //for nullable primitive we will write a single byte to indicate null or not
            if (!_isWrapPossible)
                return;

            WriteInt32(0);

            if (_node.IsWrapping)
                _backingList.Append(1);
            else
                _backingList.Append(0);
        }

        private readonly ByteBuilder _backingList;
        
        private readonly PrintNode _node;
        private readonly IBinaryWriter _parent;
        private readonly Boolean _isWrapPossible;
        private Boolean _isPopped;

        [MethodImpl(256)]
        protected sealed override void Write(Byte* bytes, Int32 count) => _backingList.Append(bytes, count);

        [MethodImpl(256)]
        public override void Write(Byte[] buffer) => _backingList.Append(buffer);

        void IRemunerable<Byte[]>.Append(Byte[] data, Int32 limit) => _backingList.Append(data, limit);


        public override IBinaryWriter Pop()
        {
            _isPopped = true;

            if (_isWrapPossible)
                SetLength(_backingList.Count);

            _parent.Imbue(this);
            return _parent;
        }

        public override void Imbue(IBinaryWriter writer)
            => _backingList.Append(writer);

        public override IEnumerator<Byte> GetEnumerator() => _backingList.GetEnumerator();

        public override String ToString() => _node + //".  Index " + ParentIndex +
                                             " Byte Count: " + _backingList.Count + " Length: " +
                                             Length + " Nodes: " + Children.Count;
        

        [MethodImpl(256)]
        private void SetLength(Int64 val)
        {
            var pi = (Byte*) &val;
            
            for (var c = 0; c < 4; c++)
                _backingList[c] = pi[c];
        }

        Boolean IRemunerable.IsEmpty => _backingList.Count == 0 && Children.Count == 0;

        public override Int32 Length
        {
            get
            {
                var cnt = _backingList.Count;
                if (_isPopped)
                    return cnt;

                foreach (var node in Children)
                {
                    if (node._isPopped)
                        continue;
                    cnt += node.Length;
                }

                return cnt;
            }
        }

        public override Int32 GetDataLength() => _backingList.Count;

        void IDisposable.Dispose()
        {
            _backingList.Clear();
        }
    }
}