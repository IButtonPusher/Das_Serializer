using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Das.Serializer.Remunerators;

public interface IProtoWriter
{
   Int32 GetPackedArrayLength16<TCollection>(TCollection packedArray)
      where TCollection : IEnumerable<Int16>;

   Int32 GetPackedArrayLength32<TCollection>(TCollection packedArray)
      where TCollection : IEnumerable<Int32>;

   Int32 GetPackedArrayLength64<TCollection>(TCollection packedArray)
      where TCollection : IEnumerable<Int64>;

   /// <summary>
   ///     The amount of bytes the int would need for serialization
   /// </summary>
   /// <param name="varInt"></param>
   /// <returns></returns>
   Int32 GetVarIntLength(Int32 varInt);

   void Write(Byte[] bytes,
              Int32 count,
              Stream outStream);

   void Write(Byte[] values,
              Stream outStream);

   void WriteInt16(Int16 val,
                   Stream outStream);

   void WriteInt16(UInt16 val,
                   Stream outStream);

   void WriteInt32(Int32 value,
                   Stream outStream);

   void WriteInt32(Int64 val,
                   Stream outStream);

   void WriteInt64(Int64 val,
                   Stream outStream);

   void WriteInt64(UInt64 val,
                   Stream outStream);

   void WriteInt8(Byte value,
                  Stream outStream);

   void WriteInt8(SByte value,
                  Stream outStream);

   void WritePacked16<TCollection>(TCollection packed,
                                   Stream _outStream)
      where TCollection : IEnumerable<Int16>;


   void WritePacked32<TCollection>(TCollection packed,
                                   Stream _outStream)
      where TCollection : IEnumerable<Int32>;

   void WritePacked64<TCollection>(TCollection packed,
                                   Stream _outStream)
      where TCollection : IEnumerable<Int64>;
}