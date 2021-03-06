﻿using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Das.Serializer.Remunerators
{
    public class BinaryWriterWrapper : BinaryWriterBase<DeferredBinaryWriter>, IBinaryWriter
    {
        public BinaryWriterWrapper(Stream stream) : base(stream)
        {
        }

        protected BinaryWriterWrapper(IBinaryWriter parent)
            : base(parent)
        {
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        #if !PARTIALTRUST

        public sealed override unsafe void WriteInt16(Int16 val)
        {
            var pi = (Byte*) &val;
            Write(pi, 2);
        }

        public sealed override unsafe void WriteInt16(UInt16 val)
        {
            var pi = (Byte*) &val;
            Write(pi, 2);
        }

        public sealed override unsafe void WriteInt8(Byte val)
        {
            var pi = &val;
            Write(pi, 1);
        }

        public sealed override unsafe void WriteInt8(SByte val)
        {
            var pi = (Byte*) &val;
            Write(pi, 1);
        }

        [MethodImpl(256)]
        public sealed override unsafe void WriteInt32(Int32 val)
        {
            var pi = (Byte*) &val;
            Write(pi, 4);
        }

        public sealed override unsafe void WriteInt32(Int64 val)
        {
            var pi = (Byte*) &val;
            Write(pi, 4);
        }

        public sealed override unsafe void WriteInt64(Int64 val)
        {
            var pi = (Byte*) &val;
            Write(pi, 8);
        }

        public sealed override unsafe void WriteInt64(UInt64 val)
        {
            var pi = (Byte*) &val;
            Write(pi, 8);
        }

        #else
        public sealed override void WriteInt64(UInt64 val)
        {
            Write(BitConverter.GetBytes(val));
        }

        public sealed override void WriteInt8(Byte value)
        {
            Write(BitConverter.GetBytes(value));
        }

        public sealed override void WriteInt8(SByte value)
        {
            Write(BitConverter.GetBytes(value));
        }

        public sealed override void WriteInt16(Int16 val)
        {
            Write(BitConverter.GetBytes(val));
        }

        public sealed override void WriteInt16(UInt16 val)
        {
            Write(BitConverter.GetBytes(val));
        }

        public sealed override void WriteInt32(Int32 value)
        {
            Write(BitConverter.GetBytes(value));
        }

        public sealed override void WriteInt32(Int64 val)
        {
            Write(BitConverter.GetBytes(val));
        }

        public sealed override void WriteInt64(Int64 val)
        {
            Write(BitConverter.GetBytes(val));
        }

        #endif

        protected override DeferredBinaryWriter GetChildWriter(NodeTypes nodeType,
                                                               Boolean isWrapping)
        {
            return new(this, nodeType, isWrapping);
        }

        //protected override DeferredBinaryWriter GetChildWriter(IPrintNode node)
        //{
        //    var list = new DeferredBinaryWriter(node, this);
        //    return list;
        //}
    }
}
