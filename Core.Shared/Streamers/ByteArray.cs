using System;

namespace Das.Serializer
{
    public class ByteArray : IByteArray
    {
        public Byte[] Bytes { get; set; }

        public ByteArray(Byte[] array)
        {
            Bytes = array;
        }

        public Byte this[Int32 bytes] => Bytes[bytes];

        public Byte[] this[Int32 start, Int32 length]
        {
            get
            {
                var res = new Byte[length];
                Buffer.BlockCopy(Bytes, start, res, 0, length);
                return res;
            }
        }

        public static implicit operator ByteArray(Byte[] array) 
            => new ByteArray(array);

        public Int64 Length => Bytes.LongLength;
    }
}