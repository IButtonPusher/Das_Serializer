using System;
using System.Collections.Generic;

namespace Das.Serializer.Remunerators
{
    public interface IProtoWriter : IBinaryWriter
    {
        IProtoWriter Push();

        void Write(Byte[] bytes, Int32 count);

        /// <summary>
        /// The amount of bytes the int would need for serialization
        /// </summary>
        /// <param name="varInt"></param>
        /// <returns></returns>
        Int32 GetVarIntLength(Int32 varInt);

        Int32 GetPackedArrayLength32<TCollection>(TCollection packedArray)
            where TCollection : IEnumerable<Int32>;

        Int32 GetPackedArrayLength16<TCollection>(TCollection packedArray)
            where TCollection : IEnumerable<Int16>;

        Int32 GetPackedArrayLength64<TCollection>(TCollection packedArray)
            where TCollection : IEnumerable<Int64>;


        void WritePacked32<TCollection>(TCollection packed)
            where TCollection : IEnumerable<Int32>;

        void WritePacked16<TCollection>(TCollection packed)
            where TCollection : IEnumerable<Int16>;

        void WritePacked64<TCollection>(TCollection packed)
            where TCollection : IEnumerable<Int64>;
    }
}
