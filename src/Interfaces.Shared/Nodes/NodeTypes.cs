using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public enum NodeTypes
    {
        None = 0,

        /// <summary>
        ///     Serialize using BinaryFormatter or ToString()
        /// </summary>
        Fallback,
        Primitive,
        Object,
        Collection,

        /// <summary>
        ///     have to use default serialization for binary and string methods for text
        /// </summary>
        PropertiesToConstructor,
        Dynamic,
        StringConvertible
    }
}