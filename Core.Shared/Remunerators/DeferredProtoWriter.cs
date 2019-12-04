using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Das.Remunerators;
using Serializer.Core.Remunerators;

namespace Das.Serializer.Remunerators
{
    /// <summary>
    /// Writes to a temporary collection that is eventually merged back into the main stream (or parent deferred)
    /// along with the length of the data for fixed length deserialization
    /// </summary>
    public class DeferredProtoWriter : ProtoBufWriter, IBinaryWriter
    {
        public DeferredProtoWriter(ProtoBufWriter parent) : base(parent)
        {
            _parent = parent;
            _backingList = new ByteBuilder();
        }

        private readonly ProtoBufWriter _parent;
        private readonly ByteBuilder _backingList;
        private Boolean _isPopped;

        [MethodImpl(256)]
        protected sealed override unsafe void Write(Byte* bytes, Int32 count) => _backingList.Append(bytes, count);

        [MethodImpl(256)]
        public override void Write(Byte[] buffer) => _backingList.Append(buffer);

        void IRemunerable<Byte[]>.Append(Byte[] data, Int32 limit) => _backingList.Append(data, limit);

        [MethodImpl(256)]
        public sealed override void WriteInt8(Byte value) => _backingList.Append(value);
        

        public override void Write(Byte[] buffer, Int32 index, Int32 count)
        {
            switch (index)
            {
                case 0 when count == buffer.Length:
                    _backingList.Append(buffer);
                    break;
                case 0 when count < buffer.Length:
                    _backingList.Append(buffer, count);
                    break;
                default:
                    _backingList.Append(buffer.Skip(index), count);
                    break;
            }
        }

        public override void Imbue(IBinaryWriter writer)
            => _backingList.Append(writer);

        public override IEnumerator<Byte> GetEnumerator() => _backingList.GetEnumerator();

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

        Boolean IRemunerable.IsEmpty => _backingList.Count == 0 && Children.Count == 0;

        void IDisposable.Dispose()
        {
            _backingList.Clear();
        }

        public override IBinaryWriter Pop()
        {
            _isPopped = true;

//            if (_isWrapPossible)
//                SetLength(_backingList.Count);

            _parent.WriteInt32(Length);
            _parent.Imbue(this);
            return _parent;
        }
    }
}
