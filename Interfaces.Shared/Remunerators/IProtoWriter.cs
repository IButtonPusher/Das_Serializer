using System;

namespace Das.Serializer.Remunerators
{
    public interface IProtoWriter : IBinaryWriter
    {
        IProtoWriter Push();

        void Write(Byte[] bytes, Int32 count);
    }
}
