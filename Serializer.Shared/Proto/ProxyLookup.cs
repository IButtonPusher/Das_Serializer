using System;
using System.Threading.Tasks;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.ProtoBuf
{
    public static class ProxyLookup<T>
    {
        public static IProtoProxy<T>? Instance { get; set; }
    }
}