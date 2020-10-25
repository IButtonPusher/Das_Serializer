using System;
using System.Threading.Tasks;

namespace Das.Streamers
{
    public interface IBinaryFeeder
    {
        Boolean HasMoreBytes { get; }

        Int32 Index { get; }

        Byte[] GetBytes(Int32 count);

        Byte GetCircularReferenceIndex();

        Double GetDouble();

        Object? GetFallback(Type dataType, ref Int32 blockSize);

        Int32 GetInt32();

        Single GetInt8();

        /// <summary>
        ///     Returns the amount of bytes that the next object will use.  Advances
        ///     the byte index forward by 4 bytes
        /// </summary>
        Int32 GetNextBlockSize();

        /// <summary>
        ///     takes the next 4 bytes for length then the next N bytes and turns them into a Type
        /// </summary>
        Type GetNextType();

        Object GetPrimitive(Type type);

        T GetPrimitive<T>();

        Byte[] GetTypeBytes();

        /// <summary>
        ///     Reuses a threadlocal array whose size will be larger than the amount that is requested.
        ///     Only use when the count is tracked and the bytes will be converted to something else
        ///     zb not to set a property of type Byte[]
        /// </summary>
        Byte[] IncludeBytes(Int32 count);

        /// <summary>
        ///     Gets the next Int32 value.  If the value matches the parameter, the index is moved up,
        ///     otherwise it is restored and this method will not have altered the state of the feeder
        /// </summary>
        /// <param name="advanceIf"></param>
        /// <returns></returns>
        Int32 PeekInt32(Int32 advanceIf);
    }
}