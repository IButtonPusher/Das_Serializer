using System;
using System.IO;
using System.Linq;
using Das.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;

namespace UnitTestProject1.ProtocolBuffers
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

        [TestMethod]
        public void SimpleIntegerTest()
        {
            //prop A: index = 2, wire type = varint = 0, value = 150
            //output: 16 150 1
            //16: indexA(2) << 3 = 10 ### + wire type(0) => ### = 000 so 10000 = 16
            //150: 10010110 = M0010110 where M = MOAR BYTES and 0010110 = 22
            //1: 00000001 = T0000001 where T = TERMINATOR and 1 << 7 = 128
            //128 + 22 = 150 a PropAVal


            var msg = new SimpleMessage();
            msg.A = 150;
            Byte[] protoArr;
            SimpleMessage outMsg;


            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                protoArr = ms.ToArray();
                ms.Position = 0;
                outMsg = ProtoBuf.Serializer.Deserialize<SimpleMessage>(ms);
            }

            var srl = Serializer.GetProtoSerializer(_options);

            Byte[] dArr;
            SimpleMessage outMsg2;
            using (var ms = new MemoryStream())
            {
                srl.ToProtoStream(ms, msg);
                dArr = ms.ToArray();

                ms.Position = 0;
                outMsg2 = srl.FromProtoStream<SimpleMessage>(ms);
            }


            Assert.IsTrue(protoArr.SequenceEqual(dArr));
            var equal = SlowEquality.AreEqual(outMsg2, msg);
            Assert.IsTrue(equal);

            equal = SlowEquality.AreEqual(outMsg, outMsg2);
            Assert.IsTrue(equal);
        }

        [TestMethod]
        public void SimpleDoubleTest()
        {
            var msg = new DoubleMessage();
            DoubleMessage  outMsg2;
            msg.D = 3.14;
            Byte[] protoArr;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                protoArr = ms.ToArray();
            }

            var srl = Serializer.GetProtoSerializer(_options);

            Byte[] dArr;
            using (var ms = new MemoryStream())
            {
                srl.ToProtoStream(ms, msg);
                dArr = ms.ToArray();
                ms.Position = 0;
                outMsg2 = srl.FromProtoStream<DoubleMessage>(ms);
            }

            Assert.IsTrue(protoArr.SequenceEqual(dArr));
            var equal = SlowEquality.AreEqual(outMsg2, msg);
            Assert.IsTrue(equal);
        }

        [TestMethod]
        public void SimpleStringTest()
        {
            var msg = new StringMessage();
            StringMessage msg2;
            msg.S = "hello wärld";
            Byte[] protoArr;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                protoArr = ms.ToArray();
            }

            var srl = Serializer.GetProtoSerializer(_options);

            Byte[] dArr;
            using (var ms = new MemoryStream())
            {
                srl.ToProtoStream(ms, msg);
                dArr = ms.ToArray();
                ms.Position = 0;
                msg2 = srl.FromProtoStream<StringMessage>(ms);
            }


            Assert.IsTrue(protoArr.SequenceEqual(dArr));
            
            var equal = SlowEquality.AreEqual(msg2, msg);
            Assert.IsTrue(equal);
        }

        [TestMethod]
        public void NegativeIntegerTest()
        {
            //prop A: index = 2, wire type = varint = 0, val = -150
            //output: 16 234 254 255 255 255 255 255 255 255 1
            //16: indexA(2) << 3 = 10 ### + wire type(0) => ### = 000 so 10000 = 16

            var msg = new SimpleMessage();
            msg.A = -150;
            Byte[] protoArr;
            SimpleMessage outMsg;
            SimpleMessage outMsg3;


            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                protoArr = ms.ToArray();
                ms.Position = 0;
                outMsg = ProtoBuf.Serializer.Deserialize<SimpleMessage>(ms);
            }

            var srl = Serializer.GetProtoSerializer(_options);

            Byte[] dArr;
            SimpleMessage outMsg2;
            using (var ms = new MemoryStream())
            {
                srl.ToProtoStream(ms, msg);
                dArr = ms.ToArray();

                ms.Position = 0;
                outMsg2 = srl.FromProtoStream<SimpleMessage>(ms);

                ms.Position = 0;
                outMsg3 = ProtoBuf.Serializer.Deserialize<SimpleMessage>(ms);
            }


            Assert.IsTrue(protoArr.SequenceEqual(dArr));
            var equal = SlowEquality.AreEqual(outMsg2, msg);
            Assert.IsTrue(equal);

            equal = SlowEquality.AreEqual(outMsg, msg);
            Assert.IsTrue(equal);

            equal = SlowEquality.AreEqual(outMsg3, msg);
            Assert.IsTrue(equal);
        }

        [TestMethod]
        public void MultiPropTest()
        {
            var msg = new MultiPropMessage();
            msg.A = 150;
            msg.S = "hello world";
            Byte[] protoArr;
            MultiPropMessage outMsg;
            MultiPropMessage outMsg3;


            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                protoArr = ms.ToArray();
                ms.Position = 0;
                outMsg = ProtoBuf.Serializer.Deserialize<MultiPropMessage>(ms);
            }

            var srl = Serializer.GetProtoSerializer(_options);

            Byte[] dArr;
            MultiPropMessage outMsg2;
            using (var ms = new MemoryStream())
            {
                srl.ToProtoStream(ms, msg);
                dArr = ms.ToArray();

                ms.Position = 0;
                outMsg2 = srl.FromProtoStream<MultiPropMessage>(ms);

                ms.Position = 0;
                outMsg3 = ProtoBuf.Serializer.Deserialize<MultiPropMessage>(ms);
            }
           
            Assert.AreEqual(protoArr.Length, dArr.Length);

            var equal = SlowEquality.AreEqual(outMsg2, msg);
            Assert.IsTrue(equal);

            equal = SlowEquality.AreEqual(outMsg, msg);
            Assert.IsTrue(equal);

            equal = SlowEquality.AreEqual(outMsg3, msg);
            Assert.IsTrue(equal);
        }

        [TestMethod]
        public void ComposedTest()
        {
            var msg = new ComposedMessage();
            msg.MultiPropMessage = new MultiPropMessage();
            msg.A = 150;
            msg.MultiPropMessage.S = "hello world";
            msg.MultiPropMessage.A = 5;
            Byte[] protoArr;
            ComposedMessage outMsg;
            ComposedMessage outMsg3;


            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                protoArr = ms.ToArray();
                ms.Position = 0;
                outMsg = ProtoBuf.Serializer.Deserialize<ComposedMessage>(ms);
            }

            var srl = Serializer.GetProtoSerializer(_options);

            Byte[] dArr;
            ComposedMessage outMsg2;
            using (var ms = new MemoryStream())
            {
                srl.ToProtoStream(ms, msg);
                dArr = ms.ToArray();

                ms.Position = 0;
                outMsg2 = srl.FromProtoStream<ComposedMessage>(ms);

                ms.Position = 0;
                outMsg3 = ProtoBuf.Serializer.Deserialize<ComposedMessage>(ms);
            }
           
            Assert.AreEqual(protoArr.Length, dArr.Length);

            var equal = SlowEquality.AreEqual(outMsg2, msg);
            Assert.IsTrue(equal);

            equal = SlowEquality.AreEqual(outMsg, msg);
            Assert.IsTrue(equal);

            equal = SlowEquality.AreEqual(outMsg3, msg);
            Assert.IsTrue(equal);
        }
    }
}
