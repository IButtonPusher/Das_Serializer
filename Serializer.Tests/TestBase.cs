﻿using System;
using System.Threading;
using Das.Serializer;
using Das.Serializer.ProtoBuf;
using ProtoBuf;

namespace Serializer.Tests
{
    public abstract class TestBase
    {
        private DasSerializer _serializer;
        protected DasSerializer Serializer => _serializer ?? (_serializer = new DasSerializer());

        protected IProtoSerializer ProtoSerializer;

        public ProtoDynamicProvider<ProtoMemberAttribute> TypeProvider;

        public TestBase()
        {
            Settings = new ProtoBufOptions<ProtoMemberAttribute>(p => p.Tag);
            ProtoSerializer = Serializer.GetProtoSerializer(Settings);
            TypeProvider = new ProtoDynamicProvider<ProtoMemberAttribute>(Settings,
                Serializer.TypeManipulator, Serializer.ObjectInstantiator);
        }

        public ProtoBufOptions<ProtoMemberAttribute> Settings { get; set; }
        
    }
}
