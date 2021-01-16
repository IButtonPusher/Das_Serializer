using System;
using System.IO;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public interface IProtoProxy<T>
    {
        Boolean IsReadOnly { get; }

        void Print(T obj, Stream target);

        T Scan(Stream stream);

        T Scan(Stream stream, Int64 byteCount);
    }
}