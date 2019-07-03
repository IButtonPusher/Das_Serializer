using System;

namespace Das.Serializer
{
    public interface IByteArray
    {
        // ReSharper disable once UnusedMember.Global
        Byte this[Int32 bytes]  { get; }

        Byte[] this[Int32 start, Int32 length] { get; }

        // ReSharper disable once UnusedMember.Global
        Int64 Length { get; }
    }
}
