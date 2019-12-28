using System;
using Das;
using Das.Serializer;
using ProtoBuf;

namespace UnitTestProject1
{
    public abstract class TestBase
    {
        private DasSerializer _serializer;
        protected DasSerializer Serializer => _serializer ?? (_serializer = new DasSerializer());

        protected IProtoSerializer ProtoSerializer;

        public TestBase()
        {
            var settings = new ProtoBufOptions<ProtoMemberAttribute>(
                protoMember => protoMember.Tag);
            ProtoSerializer = Serializer.GetProtoSerializer(settings);
        }
    }
}
