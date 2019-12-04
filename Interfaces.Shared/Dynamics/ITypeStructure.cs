using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Das.Serializer.Objects;
// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ITypeStructure
    {
        ConcurrentDictionary<String, INamedField> MemberTypes { get; }

        Int32 PropertyCount { get; }

        SerializationDepth Depth { get; }

        void OnDeserialized(Object obj, IObjectManipulator objectManipulator);

        IEnumerable<PropertyValueNode> GetPropertyValues(Object o, ISerializationDepth depth);

        /// <summary>
        /// Returns properties and/or fields depending on specified depth
        /// </summary>
        IEnumerable<INamedField> GetMembersToSerialize(ISerializationDepth depth);

        PropertyValueNode GetPropertyValue(Object o, String propertyName);

        Boolean SetFieldValue(String fieldName, Object targetObj, Object fieldVal);

        Boolean SetFieldValue<T>(String fieldName, Object targetObj, Object fieldVal);

        Boolean SetValue(String propName, ref Object targetObj, Object propVal,
            SerializationDepth depth);

        Boolean TryGetAttribute<TAttribute>(String propertyName, out TAttribute[] values)
            where TAttribute : Attribute;
    }
}