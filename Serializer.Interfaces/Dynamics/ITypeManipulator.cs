using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Das.Serializer
{
    public interface ITypeManipulator : ITypeCore
    {
        VoidMethod GetAdder(IEnumerable collection, Type type = null);

        PropertySetter CreateSetMethod(MemberInfo memberInfo);

        Func<Object, Object> CreatePropertyGetter(Type targetType,
            PropertyInfo propertyInfo);

        Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
            out Action<Object, Object> setter);

        Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo);

        Action<Object, Object> CreateFieldSetter(FieldInfo fieldInfo);

        VoidMethod CreateMethodCaller(MethodInfo method);

        Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method);

        MethodInfo GetAddMethod<T>(IEnumerable<T> collection);

        Type GetPropertyType(Type classType, String propName);

        /// <summary>
        /// read/write properties. Read only don't count for this number
        /// </summary>
        Int32 PropertyCount(Type type);

        ITypeStructure GetStructure(Type type, SerializationDepth depth);

        ITypeStructure GetStructure<T>(SerializationDepth depth);

        /// <summary>
        /// Recursive through base types without duplicates
        /// </summary>
        IEnumerable<MemberInfo> GetPropertiesToSerialize(Type type, SerializationDepth depth);

        Type InstanceMemberType(MemberInfo info);

        IEnumerable<MethodInfo> GetInterfaceMethods(Type type);

        IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type);
    }
}