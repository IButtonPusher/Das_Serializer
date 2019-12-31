using System;

namespace Das.Serializer
{
    public interface IMutableState
    {
        void UpdateSettings(ISerializerSettings settings);
    }
}
