using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.Types;


namespace Das.Serializer
{
    public interface ITypeStructure : ITypeAccessor
    {
        IPropertyAccessor[] Properties { get; }

        Int32 PropertyCount { get; }

        /// <summary>
        ///     Returns properties and/or fields depending on specified depth
        /// </summary>
        IEnumerable<IMemberAccessor> GetMembersToSerialize(SerializationDepth depth);

        TProperty GetPropertyValue<TObject, TProperty>(TObject o,
                                                       String propertyName);

        Object? GetPropertyValue(Object o,
                                 String propertyName,
                                 PropertyNameFormat format = PropertyNameFormat.Default);


        IEnumerable<KeyValuePair<PropertyInfo, Object?>> IteratePropertyValues(Object o,
                                                                               ISerializationDepth depth);

        Boolean OnDeserialized(Object obj,
                               IObjectManipulator objectManipulator);

        Boolean SetFieldValue(String fieldName,
                              Object targetObj,
                              Object fieldVal);

        Boolean SetFieldValue<T>(String fieldName,
                                 Object targetObj,
                                 Object fieldVal);


        Boolean TryGetAttribute<TAttribute>(String propertyName,
                                            PropertyNameFormat format,
                                            out TAttribute value)
            where TAttribute : Attribute;

        Boolean TryGetPropertyAccessor(String propName,
                                       PropertyNameFormat format,
                                       out IPropertyAccessor accessor);



        Boolean TrySetPropertyValue(String propName,
                                    PropertyNameFormat format,
                                    ref Object targetObj,
                                    Object? propVal);

        Boolean TryGetValueForParameter(Object obj,
                                        ParameterInfo prm,
                                        SerializationDepth depth,
                                        out Object? value,
                                        out Boolean isMemberSerializable);

    }
}
