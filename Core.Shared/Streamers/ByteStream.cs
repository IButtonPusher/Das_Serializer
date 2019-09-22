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
                var res = new Byte[1];
                _stream.Read(res, bytes, 1);
                return res[0];
            }
        }

        public Byte[] this[Int32 start, Int32 length]
        {
            get
            {
                var res = new Byte[length];

                _stream.Read(res, start, length);
                return res;
            }
        }

        public Int64 Length => _stream.Length;
    }
}