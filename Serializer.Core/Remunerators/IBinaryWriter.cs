using System;
using System.Collections.Generic;
using Das.Remunerators;
using Serializer.Core.Printers;

namespace Serializer.Core.Remunerators
{
    public interface IBinaryWriter : IEnumerable<Byte>,
        IRemunerable<Byte[], Byte>, 
        IDisposable
    {
        void WriteInt8(Byte value);

        void WriteInt8(SByte value);

        void WriteInt16(Int16 val);

        void WriteInt16(UInt16 val);

        void Write(Byte[] values);

        void WriteInt32(Int32 value);

        void WriteInt32(Int64 val);

        void WriteInt64(Int64 val);

        void WriteInt64(UInt64 val);

        void Flush();

        Int32 Length { get; }
        
        Int32 SumLength { get; }

        IBinaryWriter Pop();

        IBinaryWriter Push(PrintNode node);

        void Imbue(IBinaryWriter writer);
    }
}
