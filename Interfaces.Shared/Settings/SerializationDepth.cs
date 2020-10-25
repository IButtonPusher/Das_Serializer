using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    [Flags]
    public enum SerializationDepth
    {
        None = 0,

        /// <summary>
        ///     Considers only properties with getters and setters. Private fields and properties without
        ///     setters are not serialized/deserialized.
        /// </summary>
        GetSetProperties = 1,
        GetOnlyProperties = 2,
        AllProperties = 3,
        PrivateFields = 4,
        Full = 6
    }
}