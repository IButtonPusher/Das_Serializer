using System;

namespace Das.Serializer
{
    public interface IConverterProvider
    {
        IObjectConverter ObjectConverter { get; }
    }
}