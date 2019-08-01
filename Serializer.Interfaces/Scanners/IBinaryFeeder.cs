using System;

namespace Das.Streamers
{
    public interface IBinaryFeeder
    {
        /// <summary>
        /// Returns the amount of bytes that the next object will use.  Advances
        /// the byte index forward by 4 bytes
        /// </summary>
        /// <returns></returns>
        Int32 GetNextBlockSize();

        Byte GetCircularReferenceIndex();

        Object GetPrimitive(Type type);

        T GetPrimitive<T>();

        Int32 Index { get; }

        /// <summary>
        /// takes the next 4 bytes for length then the next N bytes and turns them into a Type
        /// </summary>
        /// <returns></returns>
        Type GetNextType();

        Byte[] GetTypeBytes();

        Byte[] GetBytes(Int32 count);

        Object GetFallback(Type dataType, ref Int32 blockSize);
    }
}