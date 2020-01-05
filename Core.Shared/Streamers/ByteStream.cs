using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Das.Serializer
{
    public class ByteStream : IByteArray
    {
        private Stream _stream;
        private Int64 _length;

        [ThreadStatic] private static Byte[] _byteCache;

        public Stream Stream
        {
            get => _stream;
            set => SetStream(value);
        }

        public void SetStream(Stream value)
        {
            _stream = value;
            if (value != null)
                _length = value.Length;
            else _length = 0;
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
                var res = (Byte)_stream.ReadByte();
                return res;
            }
        }

        public void StepBack()
        {
            _stream.Position--;
        }

        public Byte[] this[Int32 start, Int32 length]
        {
            get
            {
                var res = new Byte[length];
               
               _stream.Read(res, 0, length);
                return res;
            }
        }

        public Byte GetNextByte()
        {
            return (Byte)_stream.ReadByte();
        }

        [MethodImpl(256)]
        public void DumpProtoVarInt(ref Int32 index)
        {
            Byte current;
            do
            {
                current = (Byte) _stream.ReadByte();
                index++;
            } while ((current & 0x80) != 0);
        }

        public Byte[] GetNextBytes(Int32 amount)
        {
            var res = new Byte[amount];
               
            _stream.Read(res, 0, amount);
            return res;
        }

        public Byte[] IncludeBytes(Int32 count)
        {
            if (_byteCache == null)
                _byteCache=new Byte[Math.Max(100, count)];
            else if (_byteCache.Length < count)
                _byteCache = new Byte[count];

            //_stream.Position += count;

            _stream.Read(_byteCache, 0, count);
            return _byteCache;
        }

        public Int64 Length => _length;


        public Int64 Index => _stream.Position;
    }
}