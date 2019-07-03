using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface IStreamWrapper<out T> : IEnumerable<T>, IDisposable
    {
    }
}
