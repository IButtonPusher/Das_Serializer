using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ProtoBuf;

namespace Serializer.Tests.ProtocolBuffers
{
    [ProtoContract]
    public class SimpleMessage
    {
        [ProtoMember(2)] public Byte A { get; set; }
    }

    [ProtoContract]
    public class IntPropMessage
    {
        [ProtoMember(2)] public Int32 A { get; set; }
    }

    [ProtoContract]
    public class DoubleMessage
    {
        [ProtoMember(1)] public Double D { get; set; }
    }

    [ProtoContract]
    public class StringMessage
    {
        [ProtoMember(1)] public String S { get; set; }
    }

    [ProtoContract]
    public class MultiPropMessage
    {
        [ProtoMember(1)] public String S { get; set; }

        [ProtoMember(2)] public Int32 A { get; set; }
    }

    [ProtoContract]
    public class ComposedMessage
    {
        [ProtoMember(2)] public ComposedMessage2 InnerComposed1 { get; set; }

        [ProtoMember(3)] public ComposedMessage2 InnerComposed2 { get; set; }

        [ProtoMember(1)] public Int32 A { get; set; }

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
                        A = 5,
                        S = "hello"
                    },
                    MultiPropMessage2 = new MultiPropMessage
                    {
                        A = 51,
                        S = "world"
                    }
                };

                c.InnerComposed2 = new ComposedMessage2
                {
                    A = 21,
                    MultiPropMessage1 = new MultiPropMessage
                    {
                        A = 4,
                        S = "hallo"
                    },
                    MultiPropMessage2 = new MultiPropMessage
                    {
                        A = 41,
                        S = "weld"
                    }
                };

                return c;
            }
        }

    }

    [ProtoContract]
    public class ComposedMessage2
    {
        [ProtoMember(2)] public MultiPropMessage MultiPropMessage1 { get; set; }

        [ProtoMember(3)] public MultiPropMessage MultiPropMessage2 { get; set; }

        [ProtoMember(1)] public Int32 A { get; set; }
    }


    [ProtoContract]
    public class ByteArrayMessage
    {
        [ProtoMember(1)] public Byte[] ByteArray { get; set; }
    }

    [ProtoContract]
    public class DictionaryPropertyMessage
    {
        [ProtoMember(1)] public Dictionary<Int32, String> Dictionary1 { get; set; }

        [ProtoMember(2)] public ConcurrentDictionary<Int64, Single> Dictionary2 { get; set; }

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
    }

}
