using System;
using Das.Streamers;

namespace Das.Serializer
{
    public interface IProtoFeeder : IBinaryFeeder
    {
        void Push(Int32 length);

        void Pop();
    }
}
