using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Das.Remunerators;
using Serializer.Core.Printers;

namespace Serializer.Core.Remunerators
{
    public class BinaryWriterWrapper : BinaryWriter, IBinaryWriter
    {
        // ReSharper disable once CollectionNeverQueried.Local
        protected readonly List<BinaryListWriter> Children =
            new List<BinaryListWriter>();

        private readonly BinaryWriterWrapper _parent;

        public BinaryWriterWrapper(Stream stream) : base(stream) { }

        protected BinaryWriterWrapper(BinaryWriterWrapper parent)
            : base(parent.OutStream)
        {
            _parent = parent;
        }

        public IBinaryWriter Push(PrintNode node)
        {
            var list = new BinaryListWriter(node, this, Length);
            Children.Add(list);
            return list;
        }

        public virtual void Imbue(IBinaryWriter writer)
        {
            foreach (var b in writer)
                OutStream.WriteByte(b);
        }

        protected virtual unsafe void Write(byte* bytes, int count)
        {
            for (var c = 0; c < count; c++)
                OutStream.WriteByte(bytes[c]);
        }

        public virtual IEnumerator<byte> GetEnumerator()
        {
            foreach (var node in Children)
                foreach (var b in node)
                    yield return b;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();



        public unsafe void WriteInt16(short val)
        {
            var pi = (byte*)&val;
            Write(pi, 2);
        }

        public unsafe void WriteInt16(ushort val)
        {
            var pi = (byte*)&val;
            Write(pi, 2);
        }

        public unsafe void WriteInt8(Byte val)
        {
            var pi = &val;
            Write(pi, 1);
        }

        public unsafe void WriteInt8(SByte val)
        {
            var pi = (byte*)&val;
            Write(pi, 1);
        }

        [MethodImpl(256)]
        public unsafe void WriteInt32(int val)
        {
            var pi = (byte*)&val;
            Write(pi, 4);
        }

        public unsafe void WriteInt32(long val)
        {
            var pi = (byte*)&val;
            Write(pi, 4);
        }

        public unsafe void WriteInt64(Int64 val)
        {
            var pi = (byte*)&val;
            Write(pi, 8);
        }

        public unsafe void WriteInt64(UInt64 val)
        {
            var pi = (byte*)&val;
            Write(pi, 8);
        }

        public virtual Int32 Length => GetLengthImpl();

        public Int32 SumLength
        {
            get
            {
                var current = _parent;
                var adding = 0;
                while (current != null)
                {
                    adding += current.GetLengthImpl();
                    current = current._parent;
                }

                return adding;
            }
        }

        protected virtual Int32 GetLengthImpl() => (Int32)OutStream.Length;

        void IRemunerable<byte[], byte>.Append(byte data)
        {
            throw new NotImplementedException();
        }

        bool IRemunerable.IsEmpty => BaseStream.Length == 0;

      
        public virtual IBinaryWriter Pop() => this;

        void IRemunerable<byte[]>.Append(byte[] data) => Write(data);
        

        void IRemunerable<byte[]>.Append(byte[] data, int limit)
            => Write(data, 0, limit);

    }
}
