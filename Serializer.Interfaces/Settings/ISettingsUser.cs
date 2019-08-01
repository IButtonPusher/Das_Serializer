using System;

namespace Das.Serializer
{
    public interface ISettingsUser
    {
        ISerializerSettings Settings { get; }
    }
}