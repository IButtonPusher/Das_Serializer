using System;
using ProtoBuf;

namespace UnitTestProject1.ProtocolBuffers
{
    [ProtoContract]
    public class SimpleMessage
    {
        [ProtoMember(2)]
        public Byte A {get; set; }
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
        [ProtoMember(1)]
        public String S { get; set; }

        [ProtoMember(2)]
        public Int32 A {get; set; }
    }

    [ProtoContract]
    public class ComposedMessage
    {
        [ProtoMember(2)]
        public MultiPropMessage MultiPropMessage { get; set; }

        [ProtoMember(1)]
        public Int32 A {get; set; }
    }

    [ProtoContract]
    public class ByteArrayMessage
    {
        [ProtoMember(1)]
        public Byte[] ByteArray {get; set; }
    }
}
