using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface IObjectManipulator
    {
        /// <summary>
        ///     Tries to cast or convert as IConvertible or via an implicit type conversion.
        /// </summary>
        /// <exception cref="InvalidCastException">
        ///     The object was not IConvertible and no implicit
        ///     conversion exists
        /// </exception>
        T CastDynamic<T>(Object o);

        /// <summary>
        ///     Tries to cast or convert as IConvertible. Otherwise creates a new T
        ///     and deep copies all compatible property values
        /// </summary>
        T CastDynamic<T>(Object o,
                         IObjectConverter converter,
                         ISerializerSettings settings);

        Object Func(Object obj,
                    String funcName,
                    Object[] parameters,
                    BindingFlags flags = BindingFlags.Public | BindingFlags.Instance);

        Object GenericFunc(Object obj,
                           String funcName,
                           Object[] parameters,
                           Type[] genericParameters,
                           BindingFlags flags = BindingFlags.Public
                                                | BindingFlags.Instance);

        void GenericMethod(Object obj,
                           String methodName,
                           Type[] genericParameters,
                           Object[] parameters,
                           BindingFlags flags = BindingFlags.Public | BindingFlags.Instance);

        IProperty? GetPropertyResult(Object o,
                                     Type asType,
                                     String propertyName);

        IPropertyValueIterator<IProperty> GetPropertyResults(IValueNode obj,
                                                             ISerializationDepth depth);

        T GetPropertyValue<T>(Object obj,
                              String propertyName);

        Object? GetPropertyValue(Object obj,
                                 String propertyName);


        IEnumerable<IProperty> GetPropertyValues<T>(T obj);

        void Method(Object obj,
                    String methodName,
                    Object[] parameters,
                    BindingFlags flags = BindingFlags.Public | BindingFlags.Instance);

        Boolean SetFieldValue(Type classType,
                              String fieldName,
                              Object targetObj,
                              Object propVal);

        Boolean SetFieldValue<T>(Type classType,
                                 String fieldName,
                                 Object targetObj,
                                 Object fieldVal);

        // ReSharper disable once UnusedMember.Global
        void SetFieldValues<TObject>(TObject obj,
                                     Action<ITypeStructure, TObject> action);

        /// <summary>
        ///     Shallow copies property values from source to object
        /// </summary>
        void SetMutableProperties(IEnumerable<PropertyInfo> mutable,
                                  Object source,
                                  Object target);

        Boolean SetProperty(Type classType,
                            String propName,
                            ref Object targetObj,
                            Object? propVal);

        Boolean SetPropertyValue(ref Object targetObj,
                                 String propName,
                                 Object? propVal);

        /// <summary>
        ///     Tries to cast or convert as IConvertible. Otherwise returns false
        /// </summary>
        Boolean TryCastDynamic<T>(Object o,
                                  out T casted);

        Boolean TryGetPropertyValue<T>(Object obj,
                                       String propertyName,
                                       out T result);

        Boolean TryGetPropertyValue(Object obj,
                                    String propertyName,
                                    out Object result);
    }
}