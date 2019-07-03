using System;
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

        bool IsNumeric(Type myType);

        Boolean IsInstantiable(Type t);

        Boolean HasEmptyConstructor(Type t);

        Boolean TryGetNullableType(Type type, out Type primitive);

        IEnumerable<PropertyInfo> GetPublicProperties(Type type,
            Boolean numericFirst = true);
    }
}
