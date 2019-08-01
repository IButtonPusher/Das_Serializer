using System;

namespace Das.Serializer
{
    public interface IBinaryState : ISerializationState, IBinaryContext
    {
        IBinaryScanner Scanner { get; }
    }
}