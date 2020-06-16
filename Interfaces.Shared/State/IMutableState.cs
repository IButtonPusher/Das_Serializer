using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IMutableState
    {
        void UpdateSettings(ISerializerSettings settings);
    }
}