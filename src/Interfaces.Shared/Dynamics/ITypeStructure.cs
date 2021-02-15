using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ITypeStructure : ITypeStructureBase
        //IValueSetter
    {
        SerializationDepth Depth { get; }

        Dictionary<String, INamedField> MemberTypes { get; }

        Int32 PropertyCount { get; }

        /// <summary>
        ///     Returns properties and/or fields depending on specified depth
        /// </summary>
        IEnumerable<INamedField> GetMembersToSerialize(ISerializationDepth depth);

        Object? GetPropertyValue(Object o,
                                 String propertyName);


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
                                            out TAttribute value)
            where TAttribute : Attribute;

        Boolean TrySetPropertyValue(String propName,
                                    ref Object targetObj,
                                    Object? propVal,
                                    SerializationDepth depth = SerializationDepth.AllProperties);
    }
}
