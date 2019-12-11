using System;

namespace Das.Serializer
{
    public interface INamedValue : INamedField, IValueNode, IDisposable
    {
        Boolean IsEmptyInitialized { get; }
    }
}
