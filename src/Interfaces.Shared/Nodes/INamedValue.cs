using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface INamedValue : IValueNode,
                                   IDisposable
    {
        Boolean IsEmptyInitialized { get; }

        String Name { get; }

        //Type Type { get; }

        //Object Value { get; }
    }
}
