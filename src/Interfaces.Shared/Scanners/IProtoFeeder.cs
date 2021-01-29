using System;
using System.Threading.Tasks;
using Das.Streamers;

namespace Das.Serializer
{
    public interface IProtoFeeder : IBinaryFeeder
    {
        void DumpInt32();

        void GetInt32(ref Int32 result);

        void Pop();

        void Push(Int32 length);
    }
}
