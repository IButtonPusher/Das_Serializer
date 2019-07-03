using System;
using Das.Serializer;

namespace Serializer.Core
{
    public class ByteArray : IByteArray
    {
        private readonly byte[] _array;

        public ByteArray(Byte[] array)
        {
            _array = array;
        }

        public Byte this[int bytes] => _array[bytes];

        public Byte[] this[int start, int length]
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
