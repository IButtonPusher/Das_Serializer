using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Extensions
{
    public static class DtoExtensions
    {
        [MethodImpl(256)]
        public static Boolean AreDifferent(this Double d1,
                                           Double d2)
        {
            return Math.Abs(d1 - d2) > MyEpsilon;
        }

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Double d1,
                                             Double d2)
        {
            return Math.Abs(d1 - d2) < MyEpsilon;
        }

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Double? d1,
                                             Double? d2)
        {
            return d1.HasValue && d2.HasValue && Math.Abs(d1.Value - d2.Value) < MyEpsilon;
        }

        [MethodImpl(256)]
        public static Boolean IsNotZero(this Double d)
        {
            return Math.Abs(d) >= MyEpsilon;
        }

        [MethodImpl(256)]
        public static Boolean IsNotZero(this Int32 d)
        {
            return d != 0;
        }

        [MethodImpl(256)]
        public static Boolean IsZero(this Double d)
        {
            return Math.Abs(d) < MyEpsilon;
        }

        [MethodImpl(256)]
        public static Boolean IsZero(this Single d)
        {
            return Math.Abs(d) < MyEpsilon;
        }

        public static Boolean IsZero(this Int32 d)
        {
            return d == 0;
        }

        public static Boolean AreEqual(this ISerializationDepth left,
                                       ISerializationDepth right)
        {
            return left.SerializationDepth == right.SerializationDepth &&
                   left.IsOmitDefaultValues == right.IsOmitDefaultValues &&
                   left.IsRespectXmlIgnore == right.IsRespectXmlIgnore;
        }

        private const Double MyEpsilon = 0.00001;
    }
}
