using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITypeCore : ISettingsUser
    {
        /// <summary>
        ///     Searches base classes/interfaces more easily than using Type.GetProperty with
        ///     a labyrinth of BindingFlags
        /// </summary>
        PropertyInfo? FindPublicProperty(Type type,
                                         String propertyName);

        /// <summary>
        ///     if this is a generic collection of T or T[] it will return typeof(T)
        ///     otherwise returns the same type
        /// </summary>
        Type GetGermaneType(Type ownerType);

        IEnumerable<PropertyInfo> GetPublicProperties(Type type,
                                                      Boolean numericFirst = true);

        TypeConverter GetTypeConverter(Type type);

        Boolean HasEmptyConstructor(Type t);

        /// <summary>
        ///     read/write properties that can be set after object instantiation
        /// </summary>
        Boolean HasSettableProperties(Type type);

        Boolean IsAbstract(PropertyInfo propInfo);

        Boolean IsCollection(Type type);

        Boolean IsInstantiable(Type? t);

        Boolean IsLeaf(Type t,
                       Boolean isStringCounts);

        Boolean IsNumeric(Type myType);

        Boolean IsUseless(Type? t);

        Boolean TryGetEmptyConstructor(Type t,
                                       out ConstructorInfo ctor);

        Boolean TryGetNullableType(Type type,
                                   out Type? primitive);

        /// <summary>
        ///     Attempts to find a constructor that has parameters that match the name and type of
        ///     all properties with public get methods
        /// </summary>
        Boolean TryGetPropertiesConstructor(Type type,
                                            out ConstructorInfo constr);
    }
}
