﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Das.Serializer.Objects;
// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ITypeStructure
    {
        ConcurrentDictionary<String, MemberInfo> MemberTypes { get; }
        Int32 PropertyCount { get; }

        void OnDeserialized(Object obj, IObjectManipulator objectManipulator);

        IEnumerable<NamedValueNode> GetPropertyValues(Object o, ISerializationDepth depth);

        /// <summary>
        /// Returns properties and/or fields depending on specified depth
        /// </summary>
        IEnumerable<MemberInfo> GetMembersToSerialize(ISerializationDepth depth);

        NamedValueNode GetPropertyValue(Object o, String propertyName);

        Boolean SetFieldValue(String fieldName, Object targetObj, Object fieldVal);

        Boolean SetFieldValue<T>(String fieldName, Object targetObj, Object fieldVal);

        Boolean SetValue(String propName, ref Object targetObj, Object propVal,
            SerializationDepth depth);
    }
}