using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface INamedValue : INamedField, IValueNode, IDisposable
    {
        Boolean IsEmptyInitialized { get; }
    }
}