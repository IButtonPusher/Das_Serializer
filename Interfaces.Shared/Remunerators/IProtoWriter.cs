using System;

namespace Das.Serializer.Remunerators
{
    public interface IProtoWriter : IBinaryWriter
    {
        IProtoWriter Push();
    }
}
