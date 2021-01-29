using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;

namespace Das.Streamers
{
    public class StreamStreamer : IStreamWrapper<Char>
    {
        public StreamStreamer(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerator<Char> GetEnumerator()
        {
            var bufferSize = 1024;
            var offset = 0;
            _stream.Position = 0;
            var buffer = new Byte[bufferSize];
            var found = _stream.Read(buffer, offset, bufferSize);
            var encoding = buffer.GetEncoding();
            do
            {
                foreach (var c in encoding.GetChars(buffer, offset, found))
                {
                    yield return c;
                }

                found = _stream.Read(buffer, offset, bufferSize);
            } while (found > 0);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        //private static Encoding GetEncoding(Byte[] bom)
        //{
        //    if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
        //    if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
        //    if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
        //    if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
        //    if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
        //    return Encoding.ASCII;
        //}

        public static implicit operator Char[](StreamStreamer stream)
        {
            var len = (Int32) stream._stream.Length;
            Byte[] buffer;

            using (var memoryStream = new MemoryStream(len))
            {
                stream._stream.CopyTo(memoryStream);
                buffer = memoryStream.ToArray();
            }

            //var buffer = new Byte[len];

            var encoding = buffer.GetEncoding();
            return encoding.GetChars(buffer, 0, len);
        }

        private readonly Stream _stream;
    }
}
