using System;
using System.ComponentModel;

namespace Das.Serializer
{
    public interface ISerializationContext : ISerializationCore, INodeTypeProvider
    {
        TypeConverter GetTypeConverter(Type type);
    }
}