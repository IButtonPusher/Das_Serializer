using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPrimitiveScanner<in T>
    {
        Object GetValue(T input, Type type);
    }
}