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

        void StepBack();

        Byte[] this[Int32 start, Int32 length] { get; }

        Byte GetNextByte();

        void DumpProtoVarInt(ref Int32 index);

        Byte[] GetNextBytes(Int32 amount);

        
        /// <summary>
        /// Reuses a threadlocal array whose size will be larger than the amount that is requested.
        /// Only use when the count is tracked and the bytes will be converted to something else
        /// zb not to set a property of type Byte[]
        /// </summary>
        Byte[] IncludeBytes(Int32 count);


        // ReSharper disable once UnusedMember.Global
        Int64 Length { get; }

        Int64 Index { get; }
    }
}