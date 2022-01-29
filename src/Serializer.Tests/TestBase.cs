using System;
using System.Threading.Tasks;
using Das.Serializer;
using Das.Serializer.ProtoBuf;
using ProtoBuf;

#pragma warning disable 8618

namespace Serializer.Tests
{
    public abstract class TestBase
    {
        public TestBase()
        {
            Settings = new ProtoBufOptions<ProtoMemberAttribute>(p => p.Tag);
            ProtoSerializer = Serializer.GetProtoSerializer(Settings);
            _serializer = new DasSerializer();
            srl = _serializer;


            #if GENERATECODE
            TypeProvider = new ProtoDynamicProvider<ProtoMemberAttribute>(Settings,
                Serializer.TypeManipulator,
                Serializer.ObjectInstantiator,
                Serializer.ObjectManipulator,
                _serializer.Settings);
            #endif
        }

        public ProtoBufOptions<ProtoMemberAttribute> Settings { get; set; }

        protected DasSerializer Serializer => _serializer ?? (_serializer = new DasSerializer());


        protected static Object GetAnonymousObject()
        {
            return new
            {
                Id = 123,
                Name = "Bob",
                NumericString = "8675309",
                ZipCode = 90210
            };
        }

        protected readonly DasSerializer srl;
        protected DasSerializer _serializer;

        protected IProtoSerializer ProtoSerializer;

        #if GENERATECODE

        public ProtoDynamicProvider<ProtoMemberAttribute> TypeProvider;

        #endif
    }
}
