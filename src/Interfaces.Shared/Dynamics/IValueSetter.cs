using System;

namespace Das.Serializer
{
    public interface IValueSetter
    {
        Boolean SetValue(String propName, 
                         ref Object targetObj, 
                         Object? propVal,
                         SerializationDepth depth);

        Object? GetValue(Object o, 
                         String propertyName);
    }
}
