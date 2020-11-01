using System;
using Das.Serializer;
using Das.Serializer.ProtoBuf;
using ProtoBuf;
#pragma warning disable 8618

namespace Serializer.Tests
{
    public abstract class TestBase
    {
        private DasSerializer _serializer;

        protected DasSerializer Serializer => _serializer ?? (_serializer = new DasSerializer());

        protected IProtoSerializer ProtoSerializer;

        #if GENERATECODE

        public ProtoDynamicProvider<ProtoMemberAttribute> TypeProvider;

        #endif

        public TestBase()
        {
            Settings = new ProtoBufOptions<ProtoMemberAttribute>(p => p.Tag);
            ProtoSerializer = Serializer.GetProtoSerializer(Settings);
#if GENERATECODE
            TypeProvider = new ProtoDynamicProvider<ProtoMemberAttribute>(Settings,
                Serializer.TypeManipulator,
                Serializer.ObjectInstantiator,
                Serializer.ObjectManipulator);
#endif
        }

        public ProtoBufOptions<ProtoMemberAttribute> Settings { get; set; }

    }
}
