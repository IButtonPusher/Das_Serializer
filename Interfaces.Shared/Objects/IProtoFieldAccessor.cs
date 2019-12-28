using System;

namespace Das.Serializer
{
    public interface IProtoFieldAccessor : IProtoField
    {
        Object GetValue(Object from);
    }
}
