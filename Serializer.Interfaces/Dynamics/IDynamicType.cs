using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface IDynamicType : IDynamicAccessor
    {
        Dictionary<String, PropertySetter> PublicSetters { get; }

        Dictionary<String, Func<Object, Object>> PublicGetters { get; }

        Type ManagedType { get; }

        Boolean IsLegalValue(String forProperty, object value);

        Type GetPropertyType(String propertyName);
    }
}