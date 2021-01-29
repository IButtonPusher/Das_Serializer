using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Das.Serializer.Objects;

// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ITypeStructure : ITypeStructureBase,
                                      IValueSetter
    {
        SerializationDepth Depth { get; }

        Dictionary<String, INamedField> MemberTypes { get; }

        Int32 PropertyCount { get; }

        /// <summary>
        ///     Returns properties and/or fields depending on specified depth
        /// </summary>
        IEnumerable<INamedField> GetMembersToSerialize(ISerializationDepth depth);

        IProperty? GetPropertyValue(Object o,
                                    String propertyName);


        /// <summary>
        ///     For a collection, returns the values.  Otherwise returns the property values
        /// </summary>
        IPropertyValueIterator<IProperty> GetPropertyValues(Object o,
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
    }
}
