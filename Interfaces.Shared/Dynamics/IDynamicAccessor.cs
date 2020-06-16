using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IDynamicAccessor
    {
        Boolean SetPropertyValue(ref Object targetObj, String propName, Object propVal);

        Boolean TryGetPropertyValue(Object obj, String propertyName,
            out Object result);
    }
}