using System;
using System.IO;
using Das.Serializer;

namespace Serializer.Core
{
    public class ByteStream : IByteArray
    {
        private Stream _stream;

        public Stream Stream
        {
            get => _stream;
            set => SetStream(value);
        }

        private void SetStream(Stream value)
        {
            _stream = value;
            if (value != null)
                Length = value.Length;
            else Length = 0;
        }


        public ByteStream(Stream stream)
        {
            Stream = stream;
        }

        public ByteStream()
        {
            
        }

        public Byte this[Int32 bytes]
        {
            get
            {
                Stream.Position = bytes;
                return (Byte)Stream.ReadByte();
            }
        }

        public Byte[] this[Int32 start, Int32 length]
        {
            get
            {
                var res = new Byte[length];
                Stream.Position = start;
                Stream.Read(res, 0, length);
                return res;
            }
        }

        public Int64 Length { get; private set; }
    }
}