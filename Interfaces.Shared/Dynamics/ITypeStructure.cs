using System;
using System.Collections.Generic;
using Das.Serializer.Objects;
// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ITypeStructure
    {
        Dictionary<String, INamedField> MemberTypes { get; }

        Int32 PropertyCount { get; }

        Type Type { get; }

        SerializationDepth Depth { get; }

        Boolean OnDeserialized(Object obj, IObjectManipulator objectManipulator);

        /// <summary>
        /// For a collection, returns the values.  Otherwise returns the property values
        /// </summary>
        IPropertyValueIterator<IProperty> GetPropertyValues(Object o, 
            ISerializationDepth depth);

        /// <summary>
        /// Returns properties and/or fields depending on specified depth
        /// </summary>
        IEnumerable<INamedField> GetMembersToSerialize(ISerializationDepth depth);

        IProperty GetPropertyValue(Object o, String propertyName);

        Boolean SetFieldValue(String fieldName, Object targetObj, Object fieldVal);

        Boolean SetFieldValue<T>(String fieldName, Object targetObj, Object fieldVal);

        Boolean SetValue(String propName, ref Object targetObj, Object propVal,
            SerializationDepth depth);

        Boolean TryGetAttribute<TAttribute>(String propertyName, out TAttribute value)
            where TAttribute : Attribute;
    }
}