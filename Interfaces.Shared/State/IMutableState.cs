using System;
using Das.Serializer;

namespace Interfaces.Shared.State
{
    public interface IMutableState
    {
        void UpdateSettings(ISerializerSettings settings);
    }
}
