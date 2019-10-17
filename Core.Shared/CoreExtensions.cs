using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Das.Serializer;

// ReSharper disable UnusedMember.Global

namespace Das.CoreExtensions
{
    public static class CoreExtensionMethods
    {
        internal static ISerializerSettings Settings { get; set; }

        public static Boolean ContainsAll<T>(this IList<T> left, List<T> right)
        {
            if (left.Count != right.Count)
                return false;

            foreach (var item in left)
            {
                if (!right.Contains(item))
                    return false;
            }

            return true;
        }


        public static T[] Take<T>(this T[] arr, Int32 startIndex, Int32 length)
        {
            var res = new T[length];
            if (typeof(T).IsPrimitive)
                Buffer.BlockCopy(arr, startIndex, res, 0, length);
            else
            {
                for (var i = 0; i < length; i++)
                    res[i] = arr[i];
            }

            return res;
        }


        public static String ToString<T>(this IEnumerable<T> list, Char sep,
            Char? excludeWhenFirst = null)
        {
            if (list == null)
                return String.Empty;

            var sb = new StringBuilder();
            foreach (var i in list)
            {
                if (excludeWhenFirst != null && i.ToString()[0] == excludeWhenFirst)
                    sb.Append($"{sep}");
                else
                    sb.Append($"{i}{sep}");
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }


        public static Boolean IsString(this Type t) => t == typeof(String);


        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2, T c3, T c4, T c5, T c6, T c7) =>
            item.Equals(c7) || item.IsIn(c1, c2, c3, c4, c5, c6);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2, T c3, T c4, T c5, T c6) =>
            item.Equals(c6) || item.IsIn(c1, c2, c3, c4, c5);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2, T c3, T c4, T c5) =>
            item.Equals(c5) || item.IsIn(c1, c2, c3, c4);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2, T c3, T c4) 
            => item.Equals(c4) || item.IsIn(c1, c2, c3);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2, T c3) =>
            item.Equals(c3) || item.IsIn(c1, c2);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2) 
            => item.Equals(c1) || item.Equals(c2);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, IEnumerable<T> collection)
        {
            return collection.Any(c => Equals(c, item));
        }

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

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Single f1, Single f2)
            => Math.Abs(f1 - f2) < 0.00001f;

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Int32 i1, Double d1)
            => Convert.ToInt32(d1) == i1;

        public static Boolean Congruent<T>(this IList<T> left, IList<T> right)
        {
            if (right?.Count != left?.Count)
                return false;

            if (left == null)
                return true;

            for (var i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                    return false;
            }

            return true;
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action,
            Boolean isIncludeNulls = true)
        {
            if (source == null)
                return;

            foreach (var item in source)
            {
                if (isIncludeNulls || item != null)
                    action(item);
            }
        }
    }
}