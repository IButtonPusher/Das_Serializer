#if GENERATECODE

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Xunit;
// ReSharper disable All

namespace Serializer.Tests.ProtocolBuffers
{
    public class ProtoBufTests : TestBase
    {
        [Benchmark]
        public SimpleMessage DasSimpleMessage()
        {
            var msg = new SimpleMessage();
            msg.A = 150;

            var o = TypeProvider.GetProtoProxy<SimpleMessage>();

            SimpleMessage outMsg2;
            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);

                ms.Position = 0;

                outMsg2 = o.Scan(ms);
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

        [Benchmark]
        public DoubleMessage DasDoubleMessage()
        {
            var msg = new DoubleMessage {D = 3.14};
            var o = TypeProvider.GetProtoProxy<DoubleMessage>();
            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);

                ms.Position = 0;
                return o.Scan(ms);
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


        [Benchmark]
        public StringMessage DasStringMessage()
        {
            var msg = new StringMessage {S = "hello world"};
            var o = TypeProvider.GetProtoProxy<StringMessage>();

            //TypeProvider.DumpProxies();

            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);

                ms.Position = 0;
                return o.Scan(ms);
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


        [Benchmark]
        public DictionaryPropertyMessage DasDictionary()
        {
            var mc1 = DictionaryPropertyMessage.DefaultValue;

            var o = TypeProvider.GetProtoProxy<DictionaryPropertyMessage>();

            //TypeProvider.DumpProxies();

            using (var ms = new MemoryStream())
            {
                o.Print(mc1, ms);

                //Debug.WriteLine("DAS\r\n-----------------------------------");
                //PrintMemoryStream(ms);

                ms.Position = 0;
                return o.Scan(ms);
            }
        }

        [Benchmark]
        public DictionaryPropertyMessage ProtoNetObjectDictionary()
        {
            var msg = DictionaryPropertyMessage.DefaultValue;
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);

                //Debug.WriteLine("PNET\r\n-----------------------------------");
                //PrintMemoryStream(ms);

                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<DictionaryPropertyMessage>(ms);
            }
        }

        [Benchmark]
        public CollectionsPropertyMessage DasCollections()
        {
            var mc1 = CollectionsPropertyMessage.DefaultValue;

            var o = TypeProvider.GetProtoProxy<CollectionsPropertyMessage>();

            //TypeProvider.DumpProxies();

            using (var ms = new MemoryStream())
            {
                o.Print(mc1, ms);
                //Debug.WriteLine("DAS\r\n-----------------------------------");
                //PrintMemoryStream(ms);
                ms.Position = 0;
                return o.Scan(ms);
            }
        }

        [Benchmark]
        public CollectionsPropertyMessage ProtoCollections()
        {
            var mc1 = CollectionsPropertyMessage.DefaultValue;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, mc1);

                //Debug.WriteLine("PNET\r\n-----------------------------------");
                //PrintMemoryStream(ms);
                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<CollectionsPropertyMessage>(ms);
            }
        }

        [Benchmark]
        public PackedArrayTest DasPackedArray()
        {
            var mc1 = PackedArrayTest.DefaultValue;

            var proxy = TypeProvider.GetProtoProxy<PackedArrayTest>();
            //var proxy = new Serializer_Tests_ProtocolBuffers_PackedArrayTest(() => 
            //    new PackedArrayTest());

            using (var ms = new MemoryStream())
            {
                proxy.Print(mc1, ms);
                //Debug.WriteLine("DAS\r\n-----------------------------------");
                //PrintMemoryStream(ms);
                ms.Position = 0;
                return proxy.Scan(ms);
            }
        }

        [Benchmark]
        public PackedArrayTest ProtoPackedArray()
        {
            var mc1 = PackedArrayTest.DefaultValue;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, mc1);

                //Debug.WriteLine("PNET\r\n-----------------------------------");
                //PrintMemoryStream(ms);
                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<PackedArrayTest>(ms);
            }
        }

        [Conditional("DEBUG")]
        // ReSharper disable once UnusedMember.Local
        private static void PrintMemoryStream(MemoryStream ms)
        {
            //Debug.WriteLine(string.Join(",", ms.ToArray()) + "\r\n");

            //for (var c = 0; c < arr.Length; c++)
            //    Debug.WriteLine(arr[c]);
        }

        [Benchmark]
        public IntPropMessage DasNegativeIntegerMessage()
        {
            var msg = new IntPropMessage {A = -150};
            var o = TypeProvider.GetProtoProxy<IntPropMessage>();
            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);

                ms.Position = 0;
                return o.Scan(ms);
            }
        }

        [Benchmark]
        public IntPropMessage ProtoNetNegativeIntegerMessage()
        {
            var msg = new IntPropMessage {A = -150};
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<IntPropMessage>(ms);
            }
        }


        [Benchmark]
        public MultiPropMessage DasMultiProperties()
        {
            var msg = MultiPropMessage.GetTestOne();


            var o = TypeProvider.GetProtoProxy<MultiPropMessage>();

            //TypeProvider.DumpProxies();

            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);

                //Debug.WriteLine("DAS\r\n-----------------------------------");
                PrintMemoryStream(ms);

                ms.Position = 0;
                return o.Scan(ms);
            }
        }

        [Benchmark]
        public MultiPropMessage ProtoNetMultiProperties()
        {
            var msg = MultiPropMessage.GetTestOne();
            //var msg = new MultiPropMessage
            //{
            //    A = 26256,
            //    S = "hello world",
            //    BigInt = (Int64)Int32.MaxValue + 1
            //};
            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;
                //Debug.WriteLine("PNET\r\n-----------------------------------");
                PrintMemoryStream(ms);
                return ProtoBuf.Serializer.Deserialize<MultiPropMessage>(ms);
            }
        }

        [Benchmark]
        public ComposedMessage DasComposedMessage()
        {
            var msg = ComposedMessage.Default;
            var o = TypeProvider.GetProtoProxy<ComposedMessage>();

            //TypeProvider.DumpProxies();

            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);
                ms.Position = 0;
                var okThen = o.Scan(ms);
                return okThen;
            }
        }


        [Benchmark]
        public ComposedMessage ProtoNetComposedMessage()
        {
            var msg = ComposedMessage.Default;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);
                ms.Position = 0;


                return ProtoBuf.Serializer.Deserialize<ComposedMessage>(ms);
            }
        }

        [Benchmark]
        public ComposedCollectionMessage DasComposedCollectionMessage()
        {
            var msg = ComposedCollectionMessage.Default;
            var o = TypeProvider.GetProtoProxy<ComposedCollectionMessage>();

            //TypeProvider.DumpProxies();

            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);
                //Debug.WriteLine("DAS\r\n-----------------------------------");
                PrintMemoryStream(ms);
                ms.Position = 0;
                var okThen = o.Scan(ms);
                return okThen;
            }
        }

        [Benchmark]
        public ComposedCollectionMessage ProtoNetComposedCollectionMessage()
        {
            var msg = ComposedCollectionMessage.Default;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);

                //Debug.WriteLine("PNET\r\n-----------------------------------");
                PrintMemoryStream(ms);

                ms.Position = 0;


                return ProtoBuf.Serializer.Deserialize<ComposedCollectionMessage>(ms);
            }
        }

        [Benchmark]
        public ByteArrayMessage DasByteArray()
        {
            var msg = ByteArrayMessage;

            var o = TypeProvider.GetProtoProxy<ByteArrayMessage>();

            //TypeProvider.DumpProxies();

            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);

                ms.Position = 0;
                return o.Scan(ms);
            }
        }

        private static readonly ByteArrayMessage ByteArrayMessage = new()
        {
            ByteArray = new Byte[] {127, 0, 0, 1, 255, 123}
        };

        [Benchmark]
        public ByteArrayMessage ProtoNetByteArray()
        {
            var msg = ByteArrayMessage;

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, msg);

                ms.Position = 0;
                return ProtoBuf.Serializer.Deserialize<ByteArrayMessage>(ms);
            }
        }


        [Fact]
        public void ByteArrayTest()
        {
            var fromNet = ProtoNetByteArray();

            var fromDas1 = DasByteArray();
            var fromDas2 = DasByteArray();
            var fromDas3 = DasByteArray();

            var equal = SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas1, fromNet);
            Assert.True(equal);
        }

        [Fact]
        public void CollectionsTest()
        {
            var fromNet = ProtoCollections();

            var fromDas = DasCollections();
            var fromDas2 = DasCollections();
            var fromDas3 = DasCollections();
            var fromDas4 = DasCollections();
            var fromDas5 = DasCollections();
            var fromDas6 = DasCollections();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            equal &= SlowEquality.AreEqual(fromDas5, fromNet);
            equal &= SlowEquality.AreEqual(fromDas6, fromNet);
            Assert.True(equal);
        }

        [Fact]
        public void ListOfIntTest()
        {
            var msg = ListOfIntMessage.Instance;

            var o = TypeProvider.GetProtoProxy<ListOfIntMessage>();

            using (var ms = new MemoryStream())
            {
                o.Print(msg, ms);
                //ProtoBuf.Serializer.Serialize(ms, msg);

                //Debug.WriteLine("PNET\r\n-----------------------------------");
                //PrintMemoryStream(ms);
                ms.Position = 0;
                var res = o.Scan(ms);
                Assert.True(SlowEquality.AreEqual(msg, res));
                //var res = ProtoBuf.Serializer.Deserialize<ListOfIntMessage>(ms);

            }
        }


        [Fact]
        public void ComposedCollectionTest()
        {
            var fromNet = ProtoNetComposedCollectionMessage();
            var fromDas = DasComposedCollectionMessage();
            var fromDas2 = DasComposedCollectionMessage();
            var fromDas3 = DasComposedCollectionMessage();
            var fromDas4 = DasComposedCollectionMessage();


            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            Assert.True(equal);
        }


        [Fact]
        public void ComposedTest()
        {
            var fromNet = ProtoNetComposedMessage();
            var fromDas = DasComposedMessage();
            var fromDas2 = DasComposedMessage();
            var fromDas3 = DasComposedMessage();
            var fromDas4 = DasComposedMessage();


            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            Assert.True(equal);
        }


        [Fact]
        public void DictionaryTest()
        {
            var fromNet = ProtoNetObjectDictionary();

            var fromDas = DasDictionary();
            var fromDas2 = DasDictionary();
            var fromDas3 = DasDictionary();
            var fromDas4 = DasDictionary();
            var fromDas5 = DasDictionary();
            var fromDas6 = DasDictionary();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            equal &= SlowEquality.AreEqual(fromDas5, fromNet);
            equal &= SlowEquality.AreEqual(fromDas6, fromNet);
            Assert.True(equal);
        }


        [Fact]
        public void DynamicTypeTest()
        {
            var simpleTest = TypeProvider.GetProtoProxy<SimpleMessage>();
            Assert.NotNull(simpleTest);
        }


        [Fact]
        public void MultiPropTest()
        {
            var fromNet = ProtoNetMultiProperties();

            var fromDas = DasMultiProperties();
            var fromDas2 = DasMultiProperties();
            var fromDas3 = DasMultiProperties();
            var fromDas4 = DasMultiProperties();
            var fromDas5 = DasMultiProperties();

            //TypeProvider.DumpProxies();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            equal &= SlowEquality.AreEqual(fromDas5, fromNet);

            Assert.True(equal);
        }

        //[Fact]
        //public void PrimitivePropertiesJson()
        //{
        //    var sc = SimpleClassObjectProperty.GetNullPayload();

        //    var srl = new DasSerializer();
        //    var json = srl.ToJson(sc);

        //    var sc2 = srl.FromJson<SimpleClassObjectProperty>(json);
        //    var badProp = "";
        //    Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
        //}


        [Fact]
        public void NegativeIntegerTest()
        {
            //prop A: index = 2, wire type = varint = 0, val = -150
            //output: 16 234 254 255 255 255 255 255 255 255 1
            //16: indexA(2) << 3 = 10 ### + wire type(0) => ### = 000 so 10000 = 16

            var fromDas = DasNegativeIntegerMessage();
            var fromDas2 = DasNegativeIntegerMessage();
            var fromDas3 = DasNegativeIntegerMessage();
            var fromDas4 = DasNegativeIntegerMessage();
            var fromDas5 = DasNegativeIntegerMessage();

            var fromNet = ProtoNetNegativeIntegerMessage();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            equal &= SlowEquality.AreEqual(fromDas5, fromNet);

            Assert.True(equal);
        }

        [Fact]
        public void PackedRepeatedTest()
        {
            var fromNet = ProtoPackedArray();

            var fromDas = DasPackedArray();

            Assert.True(SlowEquality.AreEqual(fromNet, fromDas));
        }


        [Fact]
        public void SimpleDoubleTest()
        {
            var fromDas = DasDoubleMessage();
            var fromDas2 = DasDoubleMessage();
            var fromDas3 = DasDoubleMessage();
            var fromDas4 = DasDoubleMessage();
            var fromDas5 = DasDoubleMessage();
            var fromDas6 = DasDoubleMessage();

            var fromNet = ProtoNetDoubleMeessage();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            equal &= SlowEquality.AreEqual(fromDas5, fromNet);
            equal &= SlowEquality.AreEqual(fromDas6, fromNet);
            Assert.True(equal);
        }


        [Fact]
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
            Assert.True(equal);
        }


        [Fact]
        public void SimpleStringTest()
        {
            var fromDas = DasStringMessage();
            var fromDas2 = DasStringMessage();
            var fromDas3 = DasStringMessage();
            var fromDas4 = DasStringMessage();
            var fromDas5 = DasStringMessage();
            var fromDas6 = DasStringMessage();

            var fromNet = ProtoNetStringMessage();

            var equal = SlowEquality.AreEqual(fromDas, fromNet);
            equal &= SlowEquality.AreEqual(fromDas2, fromNet);
            equal &= SlowEquality.AreEqual(fromDas3, fromNet);
            equal &= SlowEquality.AreEqual(fromDas4, fromNet);
            equal &= SlowEquality.AreEqual(fromDas5, fromNet);
            equal &= SlowEquality.AreEqual(fromDas6, fromNet);
            Assert.True(equal);
        }
    }
}


#endif
