using System;
using System.Collections.Generic;
using System.IO;
using Das.Serializer;
using Serializer.Core;

namespace Das.Streamers
{
    internal class ProtoFeeder : BinaryFeeder
    {
        private Stack<Int32> _arrayIndeces;

        public ProtoFeeder(IBinaryPrimitiveScanner primitiveScanner, ISerializationCore dynamicFacade, 
            IByteArray bytes, ISerializerSettings settings, BinaryLogger logger) 
            : base(primitiveScanner, dynamicFacade, bytes, settings, logger)
        {
            ByteStream = bytes as ByteStream;
            _arrayIndeces = new Stack<Int32>();
        }

        private ByteStream _byteStream;


        public ByteStream ByteStream
        {
            get => _byteStream;
            set => SetByteStream(value);
        }

        public void SetStream(Stream stream)
        {
            ByteStream.Stream = stream;
            _currentEndIndex = (Int32)ByteStream.Length - 1;
        }

        public void Push(Int32 length)
        {
            _arrayIndeces.Push(_currentEndIndex);
            _currentEndIndex = Index + length - 1;
        }

        public void Pop()
        {
            _currentEndIndex = _arrayIndeces.Pop();
        }

        private void SetByteStream(ByteStream stream)
        {
            _byteStream = stream;
        }

        public sealed override Int32 GetInt32()
        {
            Int32 currentByte;
            var result = 0;
            var push = 0;
            
            do
            {
                currentByte = _currentBytes[_byteIndex++];
                
                result += (currentByte & 127) << push;

                if (push == 28 && result < 0)
                {
                    //read 4 bytes, value is negative
                    _byteIndex += 5;
                    break;
                }
                push += 7;
            }
            //when the 8th bit is 0 we are done.  8th bit = 1, read another byte
            while ((currentByte & 128) != 0);

            
            return result;
        }

        public override Int32 GetNextBlockSize() => GetInt32();

    }
}
