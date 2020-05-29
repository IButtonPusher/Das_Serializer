using System;

namespace Das.Serializer
{
    public interface IPrimitiveScanner<in T>
    {
        Object GetValue(T input, Type type);
    }
}