using System;
using System.Runtime.CompilerServices;

namespace Das.Extensions
{
    public static class DtoExtensions
    {
        private const Double MyEpsilon = 0.00001;

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Double d1, Double d2)
            => Math.Abs(d1 - d2) < MyEpsilon;

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Double? d1, Double? d2)
            => d1.HasValue && d2.HasValue && Math.Abs(d1.Value - d2.Value) < MyEpsilon;

        [MethodImpl(256)]
        public static Boolean IsZero(this Double d) => Math.Abs(d) < MyEpsilon;

        [MethodImpl(256)]
        public static Boolean IsZero(this Single d) => Math.Abs(d) < MyEpsilon;

        [MethodImpl(256)]
        public static Boolean IsNotZero(this Double d) => Math.Abs(d) >= MyEpsilon;

        [MethodImpl(256)]
        public static Boolean IsNotZero(this Int32 d) => d != 0;

        public static Boolean IsZero(this Int32 d) => d == 0;

        [MethodImpl(256)]
        public static Boolean AreDifferent(this Double d1, Double d2)
            => Math.Abs(d1 - d2) > MyEpsilon;
    }
}
