using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITypeManipulator : ITypeCore
    {
        Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo);

        Action<Object, Object> CreateFieldSetter(FieldInfo fieldInfo);

        Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method);

        Func<Object, Object> CreatePropertyGetter(Type targetType,
            PropertyInfo propertyInfo);

        PropertySetter CreateSetMethod(MemberInfo memberInfo);

        VoidMethod GetAdder(Type collectionType, Object exampleValue);

        VoidMethod GetAdder(IEnumerable collection, Type? collectionType = null);

        MethodInfo GetAddMethod<T>(IEnumerable<T> collection);

        MethodInfo GetAddMethod(Type collectionType);

        IEnumerable<MethodInfo> GetInterfaceMethods(Type type);

        /// <summary>
        ///     Recursive through base types without duplicates
        /// </summary>
        IEnumerable<INamedField> GetPropertiesToSerialize(Type type, ISerializationDepth depth);

        Type GetPropertyType(Type classType, String propName);

        IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type);

        // ReSharper disable once UnusedMember.Global
        ITypeStructure GetStructure<T>(ISerializationDepth depth);

        ITypeStructure GetTypeStructure(Type type, ISerializationDepth depth);

        Type InstanceMemberType(MemberInfo info);

        Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
            out Action<Object, Object> setter);

        Boolean TryGetAddMethod(Type collectionType, out MethodInfo addMethod);
    }
}