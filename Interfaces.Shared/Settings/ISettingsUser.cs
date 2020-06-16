using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ISettingsUser
    {
        ISerializerSettings Settings { get; }
    }
}