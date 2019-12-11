using System;
using System.IO;
using Serializer.Core.Remunerators;

namespace Das.Serializer.Remunerators
{
    public class ProtoBufWriter: BinaryWriterBase<DeferredProtoWriter>
    {
        private readonly IDataLender<DeferredProtoWriter, ProtoBufWriter> _protoPool;

        public new Stream OutStream
        {
            get => base.OutStream;
            set => base.OutStream = value;
        }

        private static readonly Byte[] _negative32Fill = { Byte.MaxValue, Byte.MaxValue, 
            Byte.MaxValue, Byte.MaxValue, 1};

        public ProtoBufWriter(Stream stream,
            IDataLender<DeferredProtoWriter, ProtoBufWriter> protoPool) 
            : base(stream)
        {
            _protoPool = protoPool;
        }

        public ProtoBufWriter(IDataLender<DeferredProtoWriter, ProtoBufWriter> protoPool)
        {
            _protoPool = protoPool;
        }

        protected ProtoBufWriter(ProtoBufWriter parent) : base(parent)
        {
            
        }

        public override void WriteInt8(Byte value)
        {
            OutStream.WriteByte(value);
        }

        public sealed override void WriteInt8(SByte value)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt16(Int16 val)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt16(UInt16 val)
        {
            throw new NotImplementedException();
        }

        public override IBinaryWriter Pop()
        {
            if (Parent != null)
            {
                WriteInt32(Length);
                return Parent;
            }

            var based = base.Pop();
            return based;
        }

        public sealed override void WriteInt32(Int32 value)
        {
            if (value > 0)
            {
                while (value > 0)
                {
                    var current = (Byte)(value & 127);
                    value >>= 7;
                    if (value > 0)
                        current += 128; //8th bit to specify more bytes remain
                    WriteInt8(current);
                }
            }
            else
            {
                for (var c = 0; c <= 4; c++)
                {
                    var current = (Byte)(value | 128);
                    value >>= 7;
                    WriteInt8(current);
                }
                Write(_negative32Fill, 0, 5);
            }
        }

        public override void Write(Byte[] buffer, Int32 index, Int32 count) 
            => OutStream.Write(buffer, index, count);

        public sealed override void WriteInt32(Int64 val)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt64(Int64 val)
        {
            throw new NotImplementedException();
        }

        public sealed override void WriteInt64(UInt64 val)
        {
            throw new NotImplementedException();
        }

        protected override DeferredProtoWriter GetChildWriter(IPrintNode node, IBinaryWriter parent, 
            Int32 index)
        {
            var w = _protoPool.Get(this);
            return w;
        }
        
    }
}
