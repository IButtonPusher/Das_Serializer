using System;

namespace Das.Serializer
{
    public enum ProtoFieldAction
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

        Dictionary
    }
}
