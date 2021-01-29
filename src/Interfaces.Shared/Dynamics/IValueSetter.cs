using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IValueSetter
    {
        Object? GetValue(Object o,
                         String propertyName);

        Boolean SetValue(String propName,
                         ref Object targetObj,
                         Object? propVal,
                         SerializationDepth depth);
    }
}
