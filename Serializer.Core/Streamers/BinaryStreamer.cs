using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Das.Serializer;

namespace Das.Streamers
{
    internal class BinaryStreamer : IStreamWrapper<Byte>
    {
        private readonly Stream _stream;

        public BinaryStreamer(Stream stream)
        {
            _stream = stream;
        }

        public IEnumerator<byte> GetEnumerator()
        {
            var bufferSize = 1024;
            var offset = 0;
            var buffer = new Byte[bufferSize];
            var found = _stream.Read(buffer, 0, bufferSize);

            do
            {
                offset += found;

                foreach (var b in buffer)
                    yield return b;

                found = _stream.Read(buffer, offset, bufferSize);
            } while (found > 0);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}