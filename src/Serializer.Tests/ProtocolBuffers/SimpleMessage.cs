using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProtoBuf;

// ReSharper disable All
#pragma warning disable 8618

namespace Serializer.Tests.ProtocolBuffers
{
    [ProtoContract]
    public class SimpleMessage
    {
        [ProtoMember(2)]
        public Byte A { get; set; }
    }

    [ProtoContract]
    public class IntPropMessage
    {
        [ProtoMember(2)]
        public Int32 A { get; set; }
    }

    [ProtoContract]
    public class DoubleMessage
    {
        [ProtoMember(1)]
        public Double D { get; set; }
    }

    [ProtoContract]
    public class StringMessage
    {
        [ProtoMember(1)]
        public String S { get; set; }
    }

    [ProtoContract]
    public class MultiPropMessage
    {
        [ProtoMember(4)]
        public Byte ByteProperty { get; set; }

        [ProtoMember(2)]
        public Int32 IntProperty { get; set; }

        [ProtoMember(3)]
        public Int64 BigIntProperty { get; set; }

        [ProtoMember(9)]
        public Int16 Int16Property { get; set; }

        [ProtoMember(1)]
        public String StringProperty { get; set; }

        [ProtoMember(7)]
        public Boolean BooleanProperty { get; set; }

        [ProtoMember(5)]
        public UInt32 UInt32Property { get; set; }

        [ProtoMember(6)]
        public UInt64 UInt64Property { get; set; }

        [ProtoMember(8)]
        public TimeSpan TimeSpan { get; set; }

        [ProtoMember(10)]
        public Single SingleProperty { get; set; }

        [ProtoMember(11)]
        public Double DoubleProperty { get; set; }

        [ProtoMember(12)]
        public Decimal DecimalProperty { get; set; }

        [ProtoMember(13)]
        public DateTime DateTimeProperty { get; set; }

        

        public static MultiPropMessage GetTestOne()
        {
            return new MultiPropMessage
            {
                Int16Property = 2345,
                IntProperty = 26256,
                StringProperty = "hello world",
                BigIntProperty = (Int64) Int32.MaxValue + 1,
                ByteProperty = 63,
                UInt32Property = Int32.MaxValue - 5,
                UInt64Property = (Int64) Int32.MaxValue + 5,
                BooleanProperty = true,
                TimeSpan = System.TimeSpan.FromMinutes(5),
                SingleProperty = 3.14f,
                DoubleProperty = 3.1415926,
                DecimalProperty = 3.14159265358979323M,
                DateTimeProperty = new DateTime(2012, 5, 15)
            };
        }

        public static MultiPropMessage GetTestTwo()
        {
            return new MultiPropMessage
            {
                IntProperty = 6,
                StringProperty = "Gööd ßye not world",
                BigIntProperty = -33,
                ByteProperty = 0,
                UInt32Property = 1,
                UInt64Property = (Int64) Int32.MaxValue + 5000
            };
        }
    }

    [ProtoContract]
    public class ComposedMessage
    {
        [ProtoMember(1)]
        public Int32 A { get; set; }

        public static ComposedMessage Default
        {
            get
            {
                var c = new ComposedMessage();
                c.A = 150;
                c.InnerComposed1 = new ComposedMessage2
                {
                    A = 3,
                    MultiPropMessage1 = new MultiPropMessage
                    {
                        IntProperty = 5,
                        StringProperty = "hello",
                        BigIntProperty = 54321
                    },
                    MultiPropMessage2 = new MultiPropMessage
                    {
                        IntProperty = 51,
                        StringProperty = "world",
                        BigIntProperty = 12345
                    }
                };

                c.InnerComposed2 = new ComposedMessage2
                {
                    A = 21,
                    MultiPropMessage1 = new MultiPropMessage
                    {
                        IntProperty = 4,
                        StringProperty = "hallo",
                        BigIntProperty = 80085
                    },
                    MultiPropMessage2 = new MultiPropMessage
                    {
                        IntProperty = 41,
                        StringProperty = "weld",
                        BigIntProperty = 1337
                    }
                };

                return c;
            }
        }

        [ProtoMember(2)]
        public ComposedMessage2 InnerComposed1 { get; set; }

        [ProtoMember(3)]
        public ComposedMessage2 InnerComposed2 { get; set; }
    }

    [ProtoContract]
    public class ComposedMessage2
    {
        [ProtoMember(1)]
        public Int32 A { get; set; }

        [ProtoMember(2)]
        public MultiPropMessage MultiPropMessage1 { get; set; }

        [ProtoMember(3)]
        public MultiPropMessage MultiPropMessage2 { get; set; }
    }


    [ProtoContract]
    public class ByteArrayMessage
    {
        [ProtoMember(1)]
        public Byte[] ByteArray { get; set; }
    }

    [ProtoContract]
    public class DictionaryPropertyMessage
    {
        public static DictionaryPropertyMessage DefaultValue { get; } =
            new DictionaryPropertyMessage
            {
                Dictionary1 = new Dictionary<Int32, String>
                {
                    {5, "hello"},
                    {10, "world"}
                },

                Dictionary2 = new ConcurrentDictionary<Int64, Single>
                (
                    new Dictionary<Int64, Single>
                    {
                        {Int32.MaxValue, 3.1415f},
                        {54321L, 0.0000001f}
                    })
            };

        [ProtoMember(1)]
        public Dictionary<Int32, String> Dictionary1 { get; set; }

        [ProtoMember(2)]
        public ConcurrentDictionary<Int64, Single> Dictionary2 { get; set; }
    }

    [ProtoContract]
    public class CollectionsPropertyMessage
    {
        [ProtoMember(2)]
        public Int32[] Array1 { get; set; }

        [ProtoMember(3)]
        public String[] Array2 { get; set; }


        public static CollectionsPropertyMessage DefaultValue { get; } =
            new CollectionsPropertyMessage
            {
                List1 = new List<String>
                {
                    "hello",
                    "world"
                },
                Array1 = new[]
                {
                    Int32.MaxValue, 734, 54354
                },
                Array2 = new[] {"I", "come", "from", "the", "land", "down", "under"}
            };

        [ProtoMember(1)]
        public List<String> List1 { get; set; }
    }

    [ProtoContract]
    public class PackedArrayTest
    {
        [ProtoMember(4, IsPacked = true)]
        public Int32[] Array1 { get; set; }


        public static PackedArrayTest DefaultValue { get; } =
            new PackedArrayTest
            {
                Array1 = new[]
                {
                    3, 270, 86942
                }
            };
    }

    [ProtoContract]
    public class ListOfIntMessage
    {
        public static ListOfIntMessage Instance => _instance;

        private static ListOfIntMessage _instance;

        static ListOfIntMessage()
        {
            _instance = new ListOfIntMessage();
            _instance.Items.AddRange(new[] { 3, 1, 4, 1, 5, 9, 2, 6 });
        }

        private ListOfIntMessage()
        {
            Items = new List<int>(); //{ 3, 1, 4, 1, 5, 9, 2, 6 };
        }

        [ProtoMember(1)]
        public List<Int32> Items { get; set; }
    }


    [ProtoContract]
    public class ComposedCollectionMessage
    {
        [ProtoMember(1)]
        public Int32 A { get; set; }

        public static ComposedCollectionMessage Default
        {
            get
            {
                var c = new ComposedCollectionMessage();
                c.A = 150;
                c.InnerComposed1 = new List<ComposedMessage2>
                {
                    new ComposedMessage2
                    {
                        A = 3,
                        MultiPropMessage1 = MultiPropMessage.GetTestOne(),
                        MultiPropMessage2 = MultiPropMessage.GetTestTwo()
                        //MultiPropMessage1 = new MultiPropMessage
                        //{
                        //    A = 5,
                        //    S = "hello"
                        //},
                        //MultiPropMessage2 = new MultiPropMessage
                        //{
                        //    A = 51,
                        //    S = "world"
                        //}
                    },
                    new ComposedMessage2
                    {
                        A = 4,
                        MultiPropMessage1 = new MultiPropMessage
                        {
                            IntProperty = 6,
                            StringProperty = "good bye"
                        },
                        MultiPropMessage2 = new MultiPropMessage
                        {
                            IntProperty = 51,
                            StringProperty = "not world"
                        }
                    }
                };

                c.InnerComposed2 = new[]
                {
                    new ComposedMessage2
                    {
                        A = 21,
                        MultiPropMessage1 = new MultiPropMessage
                        {
                            IntProperty = 78,
                            StringProperty = "hallo"
                        },
                        MultiPropMessage2 = new MultiPropMessage
                        {
                            IntProperty = 41,
                            StringProperty = "weld"
                        }
                    },
                    new ComposedMessage2
                    {
                        A = 22,
                        MultiPropMessage1 = new MultiPropMessage
                        {
                            IntProperty = 89,
                            StringProperty = "chüss"
                        },
                        MultiPropMessage2 = new MultiPropMessage
                        {
                            IntProperty = 1011,
                            StringProperty = "nicht weld"
                        }
                    }
                };

                return c;
            }
        }

        [ProtoMember(2)]
        public List<ComposedMessage2> InnerComposed1 { get; set; }

        [ProtoMember(3)]
        public ComposedMessage2[] InnerComposed2 { get; set; }
    }
}
