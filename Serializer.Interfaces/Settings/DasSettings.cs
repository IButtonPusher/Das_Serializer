using System;
using System.Text;
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

namespace Das.Serializer
{
    public interface ISerializerSettings : ISerializationDepth
    {
        /// <summary>
        /// If an unknown type is in the serialized data, a dynamic type can be built
        /// at runtime including properties.
        /// </summary>
        TypeNotFound NotFoundBehavior { get; set; }

        /// <summary>
        /// Particularly for Json pascal case is often used.  Setting this to false
        /// and having multiple properties with the "same" name will be problematic
        /// </summary>
        Boolean IsPropertyNamesCaseSensitive { get; set; }

        /// <summary>
        /// When getting data xml or json from a stream it may be needed to specify the
        /// encoding to ensure proper deserialization
        /// </summary>
        Encoding TextEncoding { get; set; }

        /// <summary>
        /// Specifies under which circumstances the serializer will embed type information for
        /// properties.  For xml the type of the root object is always the root node.  Choosing
        /// All will cause the Json and binary formats to wrap their output in an extra node
        /// which may make it impossible for other deserializers or services to understand the data
        /// </summary>
        TypeSpecificity TypeSpecificity { get; set; }

        CircularReference CircularReferenceBehavior { get; set; }

        

        /// <summary>
        /// Types from xml/json that are not namespace or assembly qualified will be
        /// searched for in this collection of namespaces. Defaults to just System
        /// </summary>
        String[] TypeSearchNameSpaces { get; set; }

        /// <summary>
        /// Allows control over how much nested elements in json and xml are indented.
        /// Use whitespace like spaces and tabs only
        /// </summary>
        String Indentation { get; set; }

        String NewLine { get; set; }

        /// <summary>
        /// Specifies whether ConstructorInfo methods will be turned into delegates and cached
        /// for faster object instantiation.  This can cause issues with anonymous types
        /// and is automatically disabled in contexts where one is being deserialized
        /// </summary>
        Boolean CacheTypeConstructors { get; set; }
    }
}
