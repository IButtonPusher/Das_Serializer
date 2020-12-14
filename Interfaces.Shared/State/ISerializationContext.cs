using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ISerializationContext : ISerializationCore, 
                                             ISettingsUser
    {
        IScanNodeProvider ScanNodeProvider { get; }

        //TypeConverter GetTypeConverter(Type type);
    }
}