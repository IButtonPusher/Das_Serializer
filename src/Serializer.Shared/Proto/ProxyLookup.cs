using System;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf;

public static class ProxyLookup<T>
{
   public static IProtoProxy<T>? Instance { get; set; }
}