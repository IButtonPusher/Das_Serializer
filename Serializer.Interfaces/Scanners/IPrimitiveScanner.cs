using System;

namespace Das.Scanners
{
    public interface IPrimitiveScanner<in T>
    {
        Object GetValue(T input, Type type);
    }
}