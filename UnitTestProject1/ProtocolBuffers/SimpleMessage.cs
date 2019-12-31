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
        [ProtoMember(2)] public MultiPropMessage MultiPropMessage { get; set; }

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
