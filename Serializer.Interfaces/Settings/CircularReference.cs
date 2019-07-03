using System;

namespace Das.Serializer
{
    public enum CircularReference
    {
        /// <summary>
        /// For XML and Json, does not include the guilty tag.  For binary pretends it was null
        /// </summary>
        IgnoreObject,
        /// <summary>
        /// For Json, makes an attribute with a Json path like <code>"$ref": "$.Root.Ref1.Ref2</code>
        /// which allows the reference to be restored upon deserialization.  Binary does the same.
        /// XML uses an XPath
        /// </summary>
        SerializePath,
        /// <summary>
        /// Throws an exception and the serialization fails
        /// </summary>
        ThrowException,
        NoValidation
    }
}
