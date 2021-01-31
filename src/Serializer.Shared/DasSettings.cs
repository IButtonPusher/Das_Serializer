using System;
using System.Threading.Tasks;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public class DasSettings : ISerializerSettings
    {
        static DasSettings()
        {
            _default = new DasSettings();
        }

        public DasSettings()
        {
            IsPropertyNamesCaseSensitive = true;
            TypeSpecificity = TypeSpecificity.Discrepancy;
            SerializationDepth = SerializationDepth.GetSetProperties;
            TypeSearchNameSpaces = new[] {Const.Tsystem};
            CacheTypeConstructors = true;
            //AttributeValueSurrogates = new NullAttributeValueSurrogates();
            IsUseAttributesInXml = true;
            PrintJsonPropertiesFormat = PrintPropertyFormat.Default;
        }

        /// <summary>
        ///     If an unknown type is in the serialized data, a dynamic type can be built
        ///     at runtime including properties.
        /// </summary>
        public TypeNotFound NotFoundBehavior { get; set; }

        /// <summary>
        ///     Particularly for Json pascal case is often used.  Setting this to false
        ///     and having multiple properties with the "same" name will be problematic
        /// </summary>
        public Boolean IsPropertyNamesCaseSensitive { get; set; }

        public PrintPropertyFormat PrintJsonPropertiesFormat { get; set; }


        public Boolean IsUseAttributesInXml { get; set; }

        /// <summary>
        ///     Specifies under which circumstances the serializer will embed type information for
        ///     properties.  For xml the type of the root object is always the root node.  Choosing
        ///     All will cause the Json and binary formats to wrap their output in an extra node
        ///     which may make it impossible for other deserializers or services to understand the data
        /// </summary>
        public TypeSpecificity TypeSpecificity { get; set; }

        //public IAttributeValueSurrogates AttributeValueSurrogates { get; set; }

        public CircularReference CircularReferenceBehavior { get; set; }

        /// <summary>
        ///     In Xml/Json only.  0 for integers, false for booleans, and any
        ///     val == default(ValsType) will be ommitted from the markup
        /// </summary>
        public Boolean IsOmitDefaultValues { get; set; }

        /// <summary>
        ///     Allows to set whether properties without setters and whether private fields
        ///     will be serialized.  Default is GetSetProperties
        /// </summary>
        public SerializationDepth SerializationDepth { get; set; }

        Boolean ISerializationDepth.IsRespectXmlIgnore => false;

        /// <summary>
        ///     Types from xml/json that are not namespace or assembly qualified will be
        ///     searched for in this collection of namespaces. Defaults to just System
        /// </summary>
        public String[] TypeSearchNameSpaces { get; set; }

        /// <summary>
        ///     Defines the depth of the search to resolve elements to their types when
        ///     deserializing text as JSON or XML
        /// </summary>
        public TextPropertySearchDepths PropertySearchDepth { get; set; }

        /// <summary>
        ///     Allows control over how much nested elements in json and xml are indented.
        ///     Use whitespace like spaces and tabs only
        /// </summary>
        public String Indentation { get; set; } = "  ";

        public String NewLine { get; set; } = "\r\n";

        public Boolean CacheTypeConstructors { get; set; }

        /// <summary>
        ///     Returns a mutable copy of the defaults. A new copy is generated each time
        ///     this property is accessed.
        /// </summary>
        public static DasSettings CloneDefault() => (DasSettings) _default.MemberwiseClone();


        private static readonly DasSettings _default;
    }
}
