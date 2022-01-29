using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public enum FieldAction
    {
        Primitive,
        VarInt,
        String,
        ByteArray,
        PackedArray,
        ChildObject,

        ChildObjectCollection,
        ChildObjectArray,

        ChildPrimitiveCollection,
        ChildPrimitiveArray,

        Dictionary,

        DateTime,
        NullableValueType,

        /// <summary>
        /// A special property is one where the type and name matches that of the single
        /// constructor argument for one of the declaring type's constructors
        /// </summary>
        HasSpecialProperty,

        FallbackSerializable,

        Enum
    }
}
