using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using ProtoBuf;
using UnitTestProject1;
using UnitTestProject1.ProtocolBuffers;
// ReSharper disable All

namespace Serializer.Benchmarks
{
    public class ProtoBufBenchmarks : TestBase
    {
        private readonly ProtoBufOptions<ProtoMemberAttribute> _options;
        private IProtoSerializer _serializer;

        public ProtoBufBenchmarks()
        {
            _options = new ProtoBufOptions<ProtoMemberAttribute>(
                protoMember => protoMember.Tag);
            _serializer = Serializer.GetProtoSerializer(_options);
        }

        [Benchmark]
        public SimpleMessage DasSimpleMessage()
        {
            var msg = new SimpleMessage();
            msg.A = 150;
            //Byte[] dArr;
            SimpleMessage outMsg2;
            using (var ms = new MemoryStream())
            {
                _serializer.ToProtoStream(ms, msg);

                ms.Position = 0;
                outMsg2 = _serializer.FromProtoStream<SimpleMessage>(ms);
            }

            return outMsg2;
        }

        [Benchmark]
        public SimpleMessage ProtoNetSimpleMessage()
        {
            var msg = new SimpleMessage();
            msg.A = 150;
            //Byte[] protoArr;
            SimpleMessage outMsg;


            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
              //  protoArr = ms.ToArray();
                ms.Position = 0;
                outMsg = ProtoBuf.Serializer.Deserialize<SimpleMessage>(ms);
                return outMsg;
            }
        }

        public MemoryStream Serialize(SimpleMessage msg)
        {
            var ms = new MemoryStream();
            _serializer.ToProtoStream(ms, msg);
            return ms;
        }

        public SimpleMessage Deserialize(MemoryStream ms)
        {
            ms.Position = 0;
            return _serializer.FromProtoStream<SimpleMessage>(ms);
        }

       
    }
}
