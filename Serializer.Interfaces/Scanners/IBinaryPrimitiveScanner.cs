using System;
using Das.Scanners;

namespace Das.Serializer
{
    public interface IBinaryPrimitiveScanner : IPrimitiveScanner<byte[]>
    {
        String GetString(Byte[] tempByte);

        Int32 GetInt32(Byte[] arr);
    }
}