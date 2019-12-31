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

        // ReSharper disable once UnusedMember.Global
        ITypeStructure GetStructure<T>(ISerializationDepth depth);

        /// <summary>
        /// Recursive through base types without duplicates
        /// </summary>
        IEnumerable<INamedField> GetPropertiesToSerialize(Type type, ISerializationDepth depth);

        Type InstanceMemberType(MemberInfo info);

        IEnumerable<MethodInfo> GetInterfaceMethods(Type type);

        IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type);

        ITypeStructure GetTypeStructure(Type type, ISerializationDepth depth);

        IProtoStructure GetPrintProtoStructure<TPropertyAttribute>(Type type, 
            ProtoBufOptions<TPropertyAttribute> options, ISerializationCore serializerCore)
            where TPropertyAttribute : Attribute;

        IProtoStructure GetScanProtoStructure<TPropertyAttribute>(Type type, 
            ProtoBufOptions<TPropertyAttribute> options, Int32 byteLength, 
            ISerializationCore serializerCore, IProtoFeeder byteFeeder, Int32 fieldHeader)
            where TPropertyAttribute : Attribute;
    }
}