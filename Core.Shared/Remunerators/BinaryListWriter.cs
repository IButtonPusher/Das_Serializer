﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Das.Remunerators;
using Das.Serializer;
using Serializer.Core.Printers;

namespace Serializer.Core.Remunerators
{
    public unsafe class BinaryListWriter : BinaryWriterWrapper, IBinaryWriter
    {
        public BinaryListWriter(PrintNode node, BinaryWriterWrapper parent, Int32 index)
            : base(parent)
        {
            ParentIndex = index;
            _backingList = new List<Byte>();
            _node = node;
            _parent = parent;

            _isWrapPossible = node.NodeType != NodeTypes.Primitive || node.IsWrapping;

            //for nullable primitive we will write a single byte to indicate null or not
            if (!_isWrapPossible)
                return;

            WriteInt32(0);

            if (_node.IsWrapping)
                _backingList.Add(1);
            else
                _backingList.Add(0);
        }

        [MethodImpl(256)]
        protected override void Write(Byte* bytes, Int32 count)
        {
            for (var c = 0; c < count; c++)
                _backingList.Add(bytes[c]);
        }

        public override void Write(Byte[] buffer)
        {
            var count = buffer.Length;
            for (var c = 0; c < count; c++)
                _backingList.Add(buffer[c]);
        }

        public override IBinaryWriter Pop()
        {
            _isPopped = true;

            if (_isWrapPossible)
            {
                SetLength(_backingList.Count);
            }

            _parent.Imbue(this);
            return _parent;
        }

        public override void Imbue(IBinaryWriter writer)
            => _backingList.AddRange(writer);

        void IRemunerable<Byte[]>.Append(Byte[] data, Int32 limit)
        {
            for (var c = 0; c < limit; c++)
                _backingList.Add(data[c]);
        }

        public Int32 ParentIndex { get; }

        public override IEnumerator<Byte> GetEnumerator() => _backingList.GetEnumerator();

        public override String ToString() => _node + ".  Index " + ParentIndex +
                                             " Byte Count: " + _backingList.Count + " Length: " +
                                             Length + " Nodes: " + Children.Count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private readonly List<Byte> _backingList;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly PrintNode _node;
        private readonly IBinaryWriter _parent;
        private readonly Boolean _isWrapPossible;
        private Boolean _isPopped;
        

        void IBinaryWriter.Write(Byte[] values) => _backingList.AddRange(values);

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

        protected override Int32 GetLengthImpl() => _backingList.Count;

        void IDisposable.Dispose()
        {
            _backingList.Clear();
        }
    }
}