using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace Serializer.Tests.ProtocolBuffers
{
    [TestClass]
    public class ProtoBufTests : TestBase
    {
        private readonly ProtoBufOptions<ProtoMemberAttribute> _options;

        public ProtoBufTests()
        {
            _options = new ProtoBufOptions<ProtoMemberAttribute>(
                protoMember => protoMember.Tag);
        }


        [Benchmark]
        public SimpleMessage DasSimpleMessage()
        {
            var msg = new SimpleMessage();
            msg.A = 150;
            
            SimpleMessage outMsg2;
            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, msg);

                ms.Position = 0;
                outMsg2 = ProtoSerializer.FromProtoStream<SimpleMessage>(ms);
            }

            return outMsg2;
        }

        [Benchmark]
        public SimpleMessage ProtoNetSimpleMessage()
        {
            var msg = new SimpleMessage();
            msg.A = 150;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;
                var outMsg = ProtoBuf.Serializer.Deserialize<SimpleMessage>(ms);
                return outMsg;
            }
        }

        [TestMethod]
        public void SimpleIntegerTest()
        {
            //prop A: index = 2, wire type = varint = 0, value = 150
            //output: 16 150 1
            //16: indexA(2) << 3 = 10 ### + wire type(0) => ### = 000 so 10000 = 16
            //150: 10010110 = M0010110 where M = MOAR BYTES and 0010110 = 22
            //1: 00000001 = T0000001 where T = TERMINATOR and 1 << 7 = 128
            //128 + 22 = 150 a PropAVal

            var fromNet = ProtoNetSimpleMessage();
            var fromDas = DasSimpleMessage();
            

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }

        [Benchmark]
        public DoubleMessage DasDoubleMeessage()
        {
            var msg = new DoubleMessage {D = 3.14};
            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, msg);

                ms.Position = 0;
                return ProtoSerializer.FromProtoStream<DoubleMessage>(ms);
            }
        }

        [Benchmark]
        public DoubleMessage ProtoNetDoubleMeessage()
        {
            var msg = new DoubleMessage {D = 3.14};
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);

                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<DoubleMessage>(ms);
            }
        }


        [TestMethod]
        public void SimpleDoubleTest()
        {
            var fromDas = DasDoubleMeessage();
            var fromNet = ProtoNetDoubleMeessage();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }



        [Benchmark]
        public StringMessage DasStringMessage()
        {
            var msg = new StringMessage {S = "hello world"};
            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, msg);
                ms.Position = 0;
                return ProtoSerializer.FromProtoStream<StringMessage>(ms);
            }
        }

        [Benchmark]
        public StringMessage ProtoNetStringMessage()
        {
            var msg = new StringMessage {S = "hello world"};
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<StringMessage>(ms);
            }
        }


        [TestMethod]
        public void SimpleStringTest()
        {
            var fromDas = DasStringMessage();
            var fromNet = ProtoNetStringMessage();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }


        [Benchmark]
        public DictionaryPropertyMessage DasDictionary()
        {
            var mc1 = DictionaryPropertyMessage.DefaultValue;
            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, mc1);
                //var rdrr = ms.ToArray();
                ms.Position = 0;
                return ProtoSerializer.FromProtoStream<DictionaryPropertyMessage>(ms);
            }
        }

        [Benchmark]
        public DictionaryPropertyMessage ProtoNetObjectDictionary()
        {
            var msg = DictionaryPropertyMessage.DefaultValue;
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);

                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<DictionaryPropertyMessage>(ms);
            }
        }

        [TestMethod]
        public void DictionaryTest()
        {
            var fromNet = ProtoNetObjectDictionary();
            var fromDas = DasDictionary();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }

        [Benchmark]
        public IntPropMessage DasNegativeIntegerMessage()
        {
            var msg = new IntPropMessage { A = -150 };
            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, msg);
                ms.Position = 0;
                return ProtoSerializer.FromProtoStream<IntPropMessage>(ms);
            }
        }

        [Benchmark]
        public IntPropMessage ProtoNetNegativeIntegerMessage()
        {
            var msg = new IntPropMessage { A = -150 };
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<IntPropMessage>(ms);
            }
        }


        [TestMethod]
        public void NegativeIntegerTest()
        {
            //prop A: index = 2, wire type = varint = 0, val = -150
            //output: 16 234 254 255 255 255 255 255 255 255 1
            //16: indexA(2) << 3 = 10 ### + wire type(0) => ### = 000 so 10000 = 16

            var fromDas = DasNegativeIntegerMessage();
            var fromNet = ProtoNetNegativeIntegerMessage();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }




        [Benchmark]
        public MultiPropMessage DasMultiProperties()
        {
            var msg = new MultiPropMessage
            {
                A = 150,
                S = "hello world"
            };
            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, msg);
                ms.Position = 0;
                return ProtoSerializer.FromProtoStream<MultiPropMessage>(ms);
            }
        }

        [Benchmark]
        public MultiPropMessage ProtoNetMultiProperties()
        {
            var msg = new MultiPropMessage
            {
                A = 150,
                S = "hello world"
            };
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<MultiPropMessage>(ms);
            }
        }


        [TestMethod]
        public void MultiPropTest()
        {
            var fromDas = DasMultiProperties();
            var fromNet = ProtoNetMultiProperties();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }

        [Benchmark]
        public ComposedMessage DasComposedMessage()
        {
            var msg = new ComposedMessage();
            msg.MultiPropMessage = new MultiPropMessage();
            msg.A = 150;
            msg.MultiPropMessage.S = "hello world";
            msg.MultiPropMessage.A = 5;
            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, msg);
                ms.Position = 0;
                
                return ProtoSerializer.FromProtoStream<ComposedMessage>(ms);
            }
        }


        [Benchmark]
        public ComposedMessage ProtoNetComposedMessage()
        {
            var msg = new ComposedMessage();
            msg.MultiPropMessage = new MultiPropMessage();
            msg.A = 150;
            msg.MultiPropMessage.S = "hello world";
            msg.MultiPropMessage.A = 5;
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;
                
                return ProtoBuf.Serializer.Deserialize<ComposedMessage>(ms);
            }
        }

        [TestMethod]
        public void ComposedTest()
        {
            var fromNet = ProtoNetComposedMessage();
            var fromDas = DasComposedMessage();
            

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }

        [Benchmark]
        public ByteArrayMessage DasByteArray()
        {
            var msg = new ByteArrayMessage
            {
                ByteArray = new Byte[] {127, 0, 0, 1, 255, 123}
            };

            using (var ms = new MemoryStream())
            {
                ProtoSerializer.ToProtoStream(ms, msg);
                //var rdrr = ms.ToArray();
                ms.Position = 0;
                return ProtoSerializer.FromProtoStream<ByteArrayMessage>(ms);
            }
        }

        [Benchmark]
        public ByteArrayMessage ProtoNetByteArray()
        {
            var msg = new ByteArrayMessage
            {
                ByteArray = new Byte[] {127, 0, 0, 1, 255, 123}
            };

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                //var rdrr = ms.ToArray();
                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<ByteArrayMessage>(ms);
            }
        }

      

        [TestMethod]
        public void ByteArrayTest()
        {
            var fromNet = ProtoNetByteArray();
            
            var fromDas = DasByteArray();
            fromDas = DasByteArray();
            fromDas = DasByteArray();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            Assert.IsTrue(equal);
        }

    }
}

