using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer.Remunerators;

public interface IBinaryWriter : IEnumerable<Byte>,
                                 IRemunerable<Byte[], Byte>, IStreamDelegate
{
   Int32 Length { get; }

   IBinaryWriter? Parent { get; }

   Int32 SumLength { get; }

   void Flush();

   Int32 GetDataLength();

   void Imbue(IBinaryWriter writer);

   IBinaryWriter Pop();

   IBinaryWriter Push(NodeTypes nodeType,
                      Boolean isWrapping);

   void Write(Byte[] values);

   void WriteInt16(Int16 val);

   void WriteInt16(UInt16 val);

   void WriteInt32(Int32 value);

   void WriteInt32(Int64 val);

   void WriteInt64(Int64 val);

   void WriteInt64(UInt64 val);

   void WriteInt8(Byte value);

   void WriteInt8(SByte value);
}