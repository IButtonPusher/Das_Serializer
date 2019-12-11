using System;
using System.ComponentModel;

namespace Das.Serializer
{
    public interface ISerializationContext : ISerializationCore, ISettingsUser
    {
        TypeConverter GetTypeConverter(Type type);

        IScanNodeProvider ScanNodeProvider { get; }
    }
}