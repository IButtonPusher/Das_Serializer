using System;
using Das.Serializer;

namespace Serializer.Core
{
    public class ByteArray : IByteArray
    {
        private readonly Byte[] _array;

        public ByteArray(Byte[] array)
        {
            _array = array;
        }

        public Byte this[Int32 bytes] => _array[bytes];

        public Byte[] this[Int32 start, Int32 length]
        {
            get
            {
                var res = new Byte[length];
                Buffer.BlockCopy(_array, start, res, 0, length);
                return res;
            }
        }

        public Int64 Length => _array.LongLength;
    }
}