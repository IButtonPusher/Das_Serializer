using System;

namespace Das.Streamers
{
    public interface IBinaryFeeder
    {
        /// <summary>
        /// Returns the amount of bytes that the next object will use.  Advances
        /// the byte index forward by 4 bytes
        /// </summary>
        Int32 GetNextBlockSize();

        Byte GetCircularReferenceIndex();

        Object GetPrimitive(Type type);

        T GetPrimitive<T>();

        Int32 GetInt32();

        Single GetInt8();

        Double GetDouble();

        /// <summary>
        /// Gets the next Int32 value.  If the value matches the parameter, the index is moved up,
        /// otherwise it is restored and this method will not have altered the state of the feeder
        /// </summary>
        /// <param name="advanceIf"></param>
        /// <returns></returns>
        Int32 PeekInt32(Int32 advanceIf);

        Int32 Index { get; }

        Boolean HasMoreBytes { get; }

        /// <summary>
        /// takes the next 4 bytes for length then the next N bytes and turns them into a Type
        /// </summary>
        Type GetNextType();

        Byte[] GetTypeBytes();

        Byte[] GetBytes(Int32 count);

        /// <summary>
        /// Reuses a threadlocal array whose size will be larger than the amount that is requested.
        /// Only use when the count is tracked and the bytes will be converted to something else
        /// zb not to set a property of type Byte[]
        /// </summary>
        Byte[] IncludeBytes(Int32 count);

        Object GetFallback(Type dataType, ref Int32 blockSize);
    }
}