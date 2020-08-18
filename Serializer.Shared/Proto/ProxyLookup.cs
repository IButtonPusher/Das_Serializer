using System;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public static class ProxyLookup<T>
    {
        public static IProtoProxy<T>? Instance { get;set; }

    }
}
