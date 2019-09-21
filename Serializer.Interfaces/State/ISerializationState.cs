using System;

namespace Das.Serializer
{
    /// <summary>
    /// A stateful, non threadsafe context that forms the basis of serialization/deserialization
    /// transactions
    /// </summary>
    public interface ISerializationState : ISerializationContext,
        IConverterProvider, IObjectConverter, IDisposable
    {
        new ISerializerSettings Settings { get; set; }
    }
}