using System;

namespace Das.Serializer.Scanners
{
    public interface IPrimitiveScanner<in T>
    {
        Object GetValue(T input, Type type);
    }
}