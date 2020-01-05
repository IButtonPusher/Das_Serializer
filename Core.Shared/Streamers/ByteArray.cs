using System;
using System.Runtime.CompilerServices;

namespace Das.Serializer
{
    public class ByteArray : IByteArray
    {
        private Byte[] _bytes;
        [ThreadStatic] private static Byte[] _byteCache;

        public Byte[] Bytes
        {
            get => _bytes;
            set
            {
                _bytes = value;
                _lastIndex = 0;
            }
        }

        private Int32 _lastIndex;

        public ByteArray(Byte[] array)
        {
            Bytes = array;
        }

        public Byte this[Int32 bytes]
        {
            get
            {
                _lastIndex = bytes;
                return _bytes[bytes];
            }
        }

        public Byte[] this[Int32 start, Int32 length]
        {
            get
            {
                var res = new Byte[length];
                Buffer.BlockCopy(_bytes, start, res, 0, length);
                _lastIndex = start + length;
                return res;
            }
        }

        public void StepBack()
        {
            _lastIndex--;
        }

        public static implicit operator ByteArray(Byte[] array) 
            => new ByteArray(array);

        public Byte GetNextByte()
        {
            return _bytes[_lastIndex++];
        }

        public Byte[] GetNextBytes(Int32 amount)
        {
            var res = new Byte[amount];
            Buffer.BlockCopy(_bytes, _lastIndex, res, 0, amount);
            _lastIndex += amount;
            return res;
        }

        [MethodImpl(256)]
        public void DumpProtoVarInt(ref Int32 index)
        {
            Byte current;
            do
            {
                current = _bytes[_lastIndex++];
                index++;
            } while ((current & 0x80) != 0);
        }

        public Byte[] IncludeBytes(Int32 count)
        {
            if (_byteCache == null)
                _byteCache=new Byte[Math.Max(100, count)];
            else if (_byteCache.Length < count)
                _byteCache = new Byte[count];

            Array.Copy(_bytes, 0, _byteCache, 0, count);

            return _byteCache;
        }

        public Int64 Length => _bytes.LongLength;

        public Int64 Index => _lastIndex;

    }
}