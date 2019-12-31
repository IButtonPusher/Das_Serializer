using System;

namespace Das.Serializer
{
    public class DepthConstants : ISerializationDepth
    {
        public static ISerializationDepth Full = 
            new DepthConstants(SerializationDepth.Full, true, false);

        public static ISerializationDepth AllProperties =
            new DepthConstants(SerializationDepth.Full, true, false);



        private DepthConstants(SerializationDepth depth, Boolean isOmitDefault,
            Boolean isRespectXmlIgnore)
        {
            SerializationDepth = depth;
            IsOmitDefaultValues = isOmitDefault;
            IsRespectXmlIgnore = isRespectXmlIgnore;
        }

        public Boolean IsOmitDefaultValues { get; }
        public SerializationDepth SerializationDepth { get; }
        public Boolean IsRespectXmlIgnore { get; }
    }
}
