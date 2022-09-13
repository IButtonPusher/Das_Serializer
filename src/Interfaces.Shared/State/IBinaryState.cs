using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IBinaryState : ISerializationState, IBinaryContext
    {
        IBinaryScanner Scanner { get; }
    }
}