﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    public interface ITypeCore : ISettingsUser
    {
        Boolean IsLeaf(Type t, Boolean isStringCounts);

        Boolean IsAbstract(PropertyInfo propInfo);

        Boolean IsCollection(Type type);

        Boolean IsUseless(Type t);

        Boolean IsNumeric(Type myType);

        Boolean IsInstantiable(Type t);

        Boolean HasEmptyConstructor(Type t);

        /// <summary>
        /// Attempts to find a constructor that has parameters that match the name and type of
        /// all properties with public get methods
        /// </summary>
        Boolean TryGetPropertiesConstructor(Type type, out ConstructorInfo constr);

        Boolean TryGetNullableType(Type type, out Type primitive);

        /// <summary>
        /// read/write properties that can be set after object instantiation
        /// </summary>
        Boolean HasSettableProperties(Type type);

        IEnumerable<PropertyInfo> GetPublicProperties(Type type,
            Boolean numericFirst = true);
    }
}