using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    /// <summary>
    ///     A stateful, non threadsafe context that forms the basis of serialization/deserialization
    ///     transactions
    /// </summary>
    public interface ISerializationState : ISerializationCore, IDisposable
    {
    }
}
