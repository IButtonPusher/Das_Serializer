using System;
using Das.Serializer;

namespace Das.Streamers
{
    internal class ProtoFeeder : BinaryFeeder
    {
        public ProtoFeeder(IBinaryPrimitiveScanner primitiveScanner, ISerializationCore dynamicFacade, 
            IByteArray bytes, ISerializerSettings settings, BinaryLogger logger) 
            : base(primitiveScanner, dynamicFacade, bytes, settings, logger)
        {
        }

        public override Int32 GetInt32()
        {
            Int32 currentByte;
            var result = 0;
            var push = 0;
            
            do
            {
                currentByte = _currentBytes[_byteIndex++];
                
                result += ((currentByte & 127) << push);

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
