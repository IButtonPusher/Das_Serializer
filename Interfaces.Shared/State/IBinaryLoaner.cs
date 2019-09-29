using Interfaces.Shared.State;
using System;

namespace Das.Serializer
{
    public interface IBinaryLoaner : IBinaryState, IMutableState
    {
    }
}