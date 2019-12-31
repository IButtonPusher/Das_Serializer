using System;

namespace Das.Serializer
{
    /// <summary>
    /// Allows for different types like a Stream or an actual array to be accessed the same way
    /// </summary>
    public interface IByteArray
    {
        // ReSharper disable once UnusedMember.Global
        Byte this[Int32 bytes] { get; }

        Byte[] this[Int32 start, Int32 length] { get; }

        //ArraySegment<Byte> this[Int32 start, Int32 length] { get; }

        // ReSharper disable once UnusedMember.Global
        Int64 Length { get; }
    }
}