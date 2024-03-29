﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IPropertyType
{
   Type ManagedType { get; }

   Dictionary<String, Func<Object, Object>> PublicGetters { get; }

   Dictionary<String, PropertySetter> PublicSetters { get; }

   Type? GetPropertyType(String propertyName);

   Boolean IsLegalValue(String forProperty,
                        Object? value);

   Boolean SetPropertyValue(ref Object targetObj,
                            String propName,
                            Object? propVal);
}