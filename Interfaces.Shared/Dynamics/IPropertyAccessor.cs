using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPropertyAccessor
    {
        Boolean CanRead { get; }
        
        Boolean CanWrite { get; }
        
        Type DeclaringType { get; }
        
        String PropertyPath {get;}
        
        Boolean SetPropertyValue(ref Object targetObj,
                                 Object? propVal);

        Boolean TryGetPropertyValue(Object obj,
                                    out Object result);
        
        Object? GetPropertyValue(Object obj);
    }
}