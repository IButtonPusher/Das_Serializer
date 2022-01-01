using System;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer
{
    public class DepthConstants : ISerializationDepth
    {
        private DepthConstants(SerializationDepth depth,
                               Boolean isOmitDefault,
                               Boolean isRespectXmlIgnore)
        {
            SerializationDepth = depth;
            IsOmitDefaultValues = isOmitDefault;
            IsRespectXmlIgnore = isRespectXmlIgnore;
        }

        public Boolean IsOmitDefaultValues { get; }

        public SerializationDepth SerializationDepth { get; }

        public Boolean IsRespectXmlIgnore { get; }

        public static readonly ISerializationDepth Full =
            new DepthConstants(SerializationDepth.Full, true, false);

        public static readonly ISerializationDepth AllProperties =
            new DepthConstants(SerializationDepth.AllProperties, true, false);

        public Boolean Equals(ISerializationDepth other)
        {
            return this.AreEqual(other);
        }

       
    }
}
