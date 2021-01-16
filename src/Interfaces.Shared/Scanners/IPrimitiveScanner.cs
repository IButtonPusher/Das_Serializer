using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPrimitiveScanner<in T> where T : class
    {
        Object? GetValue(T input, 
                         Type type,
                         Boolean wasInputInQuotes);
    }
}