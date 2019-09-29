using System;
using System.Collections.Generic;
using System.Reflection;
using Das.Serializer.Objects;


namespace Das.Serializer
{
    public interface IObjectManipulator : IDynamicAccessor
    {
        NamedValueNode GetPropertyResult(Object o, Type asType, String propertyName);

        T GetPropertyValue<T>(Object obj, String propertyName);

        Boolean TryGetPropertyValue<T>(Object obj, String propertyName, out T result);

        IEnumerable<NamedValueNode> GetPropertyResults(ValueNode obj, ISerializationDepth depth);

        Boolean SetFieldValue(Type classType, String fieldName, Object targetObj,
            Object propVal);

        Boolean SetFieldValue<T>(Type classType, String fieldName, Object targetObj,
            Object fieldVal);

        // ReSharper disable once UnusedMember.Global
        void SetFieldValues<TObject>(TObject obj, Action<ITypeStructure, TObject> action);

        Boolean SetProperty(Type classType, String propName, ref Object targetObj,
            Object propVal);

        void Method(Object obj, String methodName, Object[] parameters,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance);

        void GenericMethod(Object obj, String methodName,
            Type[] genericParameters, Object[] parameters,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance);

        Object Func(Object obj, String funcName, Object[] parameters,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance);

        Object GenericFunc(Object obj, String funcName, Object[] parameters,
            Type[] genericParameters, BindingFlags flags = BindingFlags.Public
                                                           | BindingFlags.Instance);

        /// <summary>
        ///  Tries to cast or convert as IConvertible. Otherwise throws an exception
        /// </summary>
        T CastDynamic<T>(Object o);

        /// <summary>
        ///  Tries to cast or convert as IConvertible. Otherwise creates a new T
        /// and deep copies all compatible property values
        /// </summary>
        T CastDynamic<T>(Object o, IObjectConverter converter, ISerializerSettings settings);

        /// <summary>
        ///  Tries to cast or convert as IConvertible. Otherwise returns false
        /// </summary>
        Boolean TryCastDynamic<T>(Object o, out T casted);
    }
}