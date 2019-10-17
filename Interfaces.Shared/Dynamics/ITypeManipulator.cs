using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    public interface ITypeManipulator : ITypeCore
    {
        VoidMethod GetAdder(Type collectionType, Object exampleValue);

        VoidMethod GetAdder(IEnumerable collection, Type collectionType = null);

        PropertySetter CreateSetMethod(MemberInfo memberInfo);

        Func<Object, Object> CreatePropertyGetter(Type targetType,
            PropertyInfo propertyInfo);

        Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
            out Action<Object, Object> setter);

        Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo);

        Action<Object, Object> CreateFieldSetter(FieldInfo fieldInfo);

        Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method);

        MethodInfo GetAddMethod<T>(IEnumerable<T> collection);

        Type GetPropertyType(Type classType, String propName);

        /// <summary>
        /// read/write properties. Read only don't count for this number
        /// </summary>
        Boolean HasSettableProperties(Type type);

        ITypeStructure GetStructure(Type type, ISerializationDepth depth);

        // ReSharper disable once UnusedMember.Global
        ITypeStructure GetStructure<T>(ISerializationDepth depth);

        /// <summary>
        /// Recursive through base types without duplicates
        /// </summary>
        IEnumerable<MemberInfo> GetPropertiesToSerialize(Type type, ISerializationDepth depth);

        Type InstanceMemberType(MemberInfo info);

        IEnumerable<MethodInfo> GetInterfaceMethods(Type type);

        IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type);
    }
}