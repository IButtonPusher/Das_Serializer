using System;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ISerializerSettings : ISerializationDepth
    {
        //IAttributeValueSurrogates AttributeValueSurrogates { get; set; }

        /// <summary>
        ///     Specifies whether ConstructorInfo methods will be turned into delegates and cached
        ///     for faster object instantiation.  This can cause issues with anonymous types
        ///     and is automatically disabled in contexts where one is being deserialized
        /// </summary>
        Boolean CacheTypeConstructors { get; set; }

        CircularReference CircularReferenceBehavior { get; set; }

        /// <summary>
        ///     Allows control over how much nested elements in json and xml are indented.
        ///     Use whitespace like spaces and tabs only
        /// </summary>
        String Indentation { get; set; }

        /// <summary>
        ///     Particularly for Json pascal case is often used.  Setting this to false
        ///     and having multiple properties with the "same" name will be problematic
        /// </summary>
        Boolean IsPropertyNamesCaseSensitive { get; set; }


        PrintPropertyFormat PrintJsonPropertiesFormat { get; set; }

        /// <summary>
        ///     Using attributes can make the markup smaller but can limit compatibility with other serializers
        /// </summary>
        Boolean IsUseAttributesInXml { get; set; }

        String NewLine { get; set; }

        /// <summary>
        ///     If an unknown type is in the serialized data, a dynamic type can be built
        ///     at runtime including properties.
        /// </summary>
        TypeNotFoundBehavior TypeNotFoundBehavior { get; set; }

        
        /// <summary>
        /// If data exists in markup but no property can be found for the known type that is being
        /// deserialized to.
        /// </summary>
        PropertyNotFoundBehavior PropertyNotFoundBehavior { get; set; }

        /// <summary>
        ///     Defines the depth of the search to resolve elements to their types when
        ///     deserializing text as JSON or XML
        /// </summary>
        TextPropertySearchDepths PropertySearchDepth { get; }


        /// <summary>
        ///     Types from xml/json that are not namespace or assembly qualified will be
        ///     searched for in this collection of namespaces. Defaults to just System
        /// </summary>
        String[] TypeSearchNameSpaces { get; set; }

        /// <summary>
        ///     Specifies under which circumstances the serializer will embed type information for
        ///     properties.  For xml the type of the root object is always the root node.  Choosing
        ///     All will cause the Json and binary formats to wrap their output in an extra node
        ///     which may make it impossible for other deserializers or services to understand the data
        /// </summary>
        TypeSpecificity TypeSpecificity { get; set; }
    }
}
