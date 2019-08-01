using System;

namespace Das.Serializer
{
    public interface IStateLoaner : ISerializationState, IDisposable
    {
    }
}