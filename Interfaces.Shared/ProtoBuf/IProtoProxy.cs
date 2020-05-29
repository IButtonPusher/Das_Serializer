using System;
using System.IO;

namespace Das.Serializer.ProtoBuf
{
    public interface IProtoProxy<T>
    {
        Boolean IsReadOnly { get; }

        //Stream OutStream { get; set; }

        void Print(T obj, Stream target);

        T Scan(Stream stream);

        T Scan(Stream stream, Int64 byteCount);
    }
}