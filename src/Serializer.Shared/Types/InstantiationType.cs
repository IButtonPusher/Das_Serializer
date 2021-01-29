using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public enum InstantiationType
    {
        NullObject,
        EmptyString,
        EmptyArray,
        DefaultConstructor,
        Emit,
        Uninitialized,
        Abstract
    }
}
