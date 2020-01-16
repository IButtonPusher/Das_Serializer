using System.IO;

namespace Das.Serializer.ProtoBuf
{
    public interface IProtoProxy<T>
    {
        Stream OutStream { get; set; }

        void Print(T obj);

        T Scan(Stream stream);
    }
}