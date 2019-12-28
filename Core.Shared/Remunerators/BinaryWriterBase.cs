using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Das.Remunerators;
using Das.Serializer;

namespace Serializer.Core.Remunerators
{
    public abstract class BinaryWriterBase<TChildWriter> : BinaryWriterBase
        where TChildWriter : IBinaryWriter
    {
        // ReSharper disable once CollectionNeverQueried.Local
        protected readonly List<TChildWriter> Children =
            new List<TChildWriter>();

        public BinaryWriterBase(Stream stream) : base(stream)
        {
        }

        protected BinaryWriterBase(IBinaryWriter parent)
            : base(parent)
        {
            
        }

        protected BinaryWriterBase(){}

        public override IEnumerator<Byte> GetEnumerator()
        {
            foreach (var node in Children)
            foreach (var b in node)
                yield return b;
        }

        public override IBinaryWriter Push(IPrintNode node)
        {
            var list = GetChildWriter(node, this, Length);
            Children.Add(list);
            return list;
        }

        protected abstract TChildWriter GetChildWriter(IPrintNode node, IBinaryWriter parent,
            Int32 index);
    }

    public abstract class BinaryWriterBase : BinaryWriter, IBinaryWriter
    {
        public abstract void WriteInt8(Byte value);
        public abstract void WriteInt8(SByte value);
        public abstract void WriteInt16(Int16 val);
        public abstract void WriteInt16(UInt16 val);
        public abstract void WriteInt32(Int32 value);
        public abstract void WriteInt32(Int64 val);
        public abstract void WriteInt64(Int64 val);
        public abstract void WriteInt64(UInt64 val);

        protected virtual unsafe void Write(Byte* bytes, Int32 count)
        {
            for (var c = 0; c < count; c++)
                OutStream.WriteByte(bytes[c]);
        }

        public virtual Int32 Length => GetDataLength();

        Stream IStreamDelegate.OutStream => OutStream;

        public Int32 SumLength
        {
            get
            {
                var current = Parent;
                var adding = 0;
                while (current != null)
                {
                    adding += current.GetDataLength();
                    current = current.Parent;
                }

                return adding;
            }
        }

        public IBinaryWriter Parent { get; protected set; }

        public abstract IBinaryWriter Push(IPrintNode node);

        public virtual Int32 GetDataLength() => (Int32) OutStream.Length;

        protected BinaryWriterBase(Stream stream) : base(stream)
        {
        }

        protected BinaryWriterBase(IBinaryWriter parent)
            : base(parent.OutStream)
        {
            Parent = parent;
        }

        protected BinaryWriterBase() {}

        public virtual void Imbue(IBinaryWriter writer)
        {
            foreach (var b in writer)
                OutStream.WriteByte(b);
        }

        public abstract IEnumerator<Byte> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Append(Byte data)
        {
            throw new NotImplementedException();
        }

        Boolean IRemunerable.IsEmpty => BaseStream.Length == 0;


        public virtual IBinaryWriter Pop() => this;

        public virtual void Append(Byte[] data) => Write(data);


        public void Append(Byte[] data, Int32 limit)
            => Write(data, 0, limit);
    }
}
