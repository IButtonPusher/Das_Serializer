using System;

namespace Das.Serializer
{
    /// <summary>
    /// For text deserialization, the amount of effort that should be
    /// expended to resolve properties.  For most use cases, the JSON name
    /// should map to an easily mappable property
    /// as should the or the XML node or tag.  When dealing with markup
    /// whose format is externally defined, it may produce better results
    /// to broaden these parameters.
    /// </summary>
    public enum TextPropertySearchDepths
    {
        /// <summary>
        /// Searches only in the type being deserialized (assuming it's known).  Does not provide
        /// enough flexibility to deserialize something like HTML where the tags like "label"
        /// refer to what are type names rather than properties.
        /// </summary>
        ResolveByPropertyName = 0,
        /// <summary>
        /// Searches as property names but also as types within a predefined collection of
        /// namespaces.  This is an appropriate option for markup deserialization
        /// </summary>
        /// <see cref="ISerializerSettings.TypeSearchNameSpaces"/>
        AsTypeInNamespacesAndSystem = 1,
        /// <summary>
        /// Will search all loaded modules for a type that matches by name alone.  Sensible
        /// when producing dynamic types where flexbility is important.  For example
        /// custom reporting where the dynamic type will be bound against grid columns
        /// </summary>
        AsTypeInLoadedModules = 2
    }
}
