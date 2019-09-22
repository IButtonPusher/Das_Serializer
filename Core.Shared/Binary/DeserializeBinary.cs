using Das.Streamers;
using System;
using System.IO;
using Serializer.Core;
using System.Diagnostics;
using Das.CoreExtensions;


namespace Das
{
    public partial class DasCoreSerializer
    {
        public T FromBytes<T>(Byte[] bytes)
        {
            Trace.WriteLine($"________DESERIALIZING {bytes.ToString(',')}");

            using (var state = StateProvider.BorrowBinary(Settings))
            {
                var bit = new BinaryIterator(bytes);
                var res = state.Scanner.Deserialize<T>(bit);
                return res;
            }
        }

        public T FromBytes<T>(FileInfo file)
        {
            using (var stream = file.OpenRead())
                return FromBytes<T>(stream);
        }

        public T FromBytes<T>(Stream stream)
        {
            using (var state = StateProvider.BorrowBinary(Settings))
            {
                var arr = new ByteStream(stream);
                return state.Scanner.Deserialize<T>(arr);
            }
        }

        public Object FromBytes(Byte[] bytes)
        {
            using (var state = StateProvider.BorrowBinary(Settings))
                return state.Scanner.Deserialize<Object>(new BinaryIterator(bytes));
        }
    }
}