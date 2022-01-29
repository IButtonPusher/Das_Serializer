using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IIntRemunerable
    {
        void Append(Boolean item);

        void Append(Byte item);

        void Append(Int16 item);

        void Append(UInt16 item);

        void Append(Int32 item);

        void Append(UInt32 item);

        void Append(Int64 item);

        void Append(UInt64 item);

        
        void Append(Single item);

        void Append(Double item);

        void Append(Decimal item);
    }
}
