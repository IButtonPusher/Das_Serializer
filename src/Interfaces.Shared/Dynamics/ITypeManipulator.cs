﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITypeManipulator : ITypeCore,
                                        IPropertyProvider
    {
        Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo);

        Func<TParent, TField> CreateFieldGetter<TParent, TField>(FieldInfo fieldInfo);

        Action<Object, Object?> CreateFieldSetter(FieldInfo fieldInfo);

        Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method);

        VoidMethod CreateMethodCaller(MethodInfo method);

        Func<Object, Object> CreatePropertyGetter(Type targetType,
                                                  PropertyInfo propertyInfo);

        Func<TObject, TProperty> CreatePropertyGetter<TObject, TProperty>(PropertyInfo propInfo);

        Func<TObject, TProperty> CreatePropertyGetter<TObject, TProperty>(String propertyName,
                                                                          out PropertyInfo propInfo);

        Func<Object, Object>? CreatePropertyGetter(Type targetType,
                                                   String propertyName,
                                                   out PropertyInfo propInfo);

        PropertySetter? CreateSetMethod(MemberInfo memberInfo);

        PropertySetter<T>? CreateSetMethod<T>(String memberName);

        PropertySetter? CreateSetMethod(Type declaringType,
                                        String memberName);

        /// <summary>
        ///     Searches the type including explicit implementations
        /// </summary>
        MethodInfo? FindInvocableMethod(Type type,
                                        String methodName,
                                        Type[] paramTypes);

        /// <summary>
        ///     Searches the type including explicit implementations
        /// </summary>
        MethodInfo? FindInvocableMethod(Type type,
                                        ICollection<String> possibleMethodNames,
                                        Type[] paramTypes);

        VoidMethod? GetAdder(Type collectionType,
                             Object exampleValue);

        VoidMethod GetAdder(IEnumerable collection,
                            Type? collectionType = null);

        MethodInfo? GetAddMethod<T>(IEnumerable<T> collection);

        MethodInfo GetAddMethod(Type collectionType);

        IEnumerable<MethodInfo> GetInterfaceMethods(Type type);

        /// <summary>
        ///     Recursive through base types without duplicates
        /// </summary>
        IEnumerable<INamedField> GetPropertiesToSerialize(Type type,
                                                          SerializationDepth depth);

        Type? GetPropertyType(Type classType,
                              String propName);

        IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type);

        // ReSharper disable once UnusedMember.Global
        ITypeStructure<T> GetTypeStructure<T>();

        ITypeStructure GetTypeStructure(Type type);

        //ITypeStructure GetTypeStructure(Type type,
        //                                ISerializationDepth depth);

        Type InstanceMemberType(MemberInfo info);

        Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
                                                out Action<Object, Object?> setter);

        Boolean TryGetAdder(IEnumerable collection,
                            out VoidMethod adder);

        Boolean TryGetAddMethod(Type collectionType,
                                out MethodInfo addMethod);
    }
}
