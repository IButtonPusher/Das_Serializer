using System;
using System.IO;
using Das.Serializer;

namespace Serializer.Core
{
    public class ByteStream : IByteArray
    {
        private readonly Stream _stream;


        public ByteStream(Stream stream)
        {
            _stream = stream;
        }

        public Byte this[Int32 bytes]
        {
            get
            {
                _stream.Position = bytes;
                return (Byte)_stream.ReadByte();
            }
        }

        public Byte[] this[Int32 start, Int32 length]
        {
            get
            {
                var res = new Byte[length];
                _stream.Position = start;
                _stream.Read(res, 0, length);
                return res;
            }
        }

        public Int64 Length => _stream.Length;
    }
}