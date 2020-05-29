using System;


namespace Das.Serializer
{
    public interface IBinaryPrimitiveScanner : IPrimitiveScanner<Byte[]>
    {
        String GetString(Byte[] tempByte);

        Int32 GetInt32(Byte[] arr);
    }
}