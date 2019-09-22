using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Das.Serializer;
using Serializer.Core;

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

        public static T[] Take<T>(this T[] arr, Int32 startIndex)
        {
            var length = arr.Length - startIndex;
            return arr.Take(startIndex, length);
        }

        public static Byte[] GetBytes(this Decimal dec)
            => TypeCore.GetBytes(dec);


        public static Decimal ToDecimal(this Byte[] bytes)
            => TypeCore.ToDecimal(bytes);


        public static Boolean IsUseless(this Type t) => t == null || t == typeof(Object);

        public static String ToString<T>(this IEnumerable<T> list, Char sep,
            Char? excludeWhenFirst = null)
        {
            if (list == null)
                return "";

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

        /// <summary>
        /// True for all IEnumerables except for string
        /// </summary>
        public static Boolean IsCollection(this Type type) =>
            typeof(IEnumerable).IsAssignableFrom(type) && !type.IsString();

        public static Boolean IsString(this Type t) => t == typeof(String);

        public static String XmlEscape(this String input) =>
            System.Security.SecurityElement.Escape(input);


        [MethodImpl(256)]
        public static Boolean In<T>(this T item, T c1, T c2, T c3, T c4, T c5, T c6, T c7) =>
            item.Equals(c7) || item.In(c1, c2, c3, c4, c5, c6);

        [MethodImpl(256)]
        public static Boolean In<T>(this T item, T c1, T c2, T c3, T c4, T c5, T c6) =>
            item.Equals(c6) || item.In(c1, c2, c3, c4, c5);

        [MethodImpl(256)]
        public static Boolean In<T>(this T item, T c1, T c2, T c3, T c4, T c5) =>
            item.Equals(c5) || item.In(c1, c2, c3, c4);

        [MethodImpl(256)]
        public static Boolean In<T>(this T item, T c1, T c2, T c3, T c4) => item.Equals(c4) || item.In(c1, c2, c3);

        [MethodImpl(256)]
        public static Boolean In<T>(this T item, T c1, T c2, T c3) => item.Equals(c3) || item.In(c1, c2);

        [MethodImpl(256)]
        public static Boolean In<T>(this T item, T c1, T c2) => item.Equals(c1) || item.Equals(c2);

        [MethodImpl(256)]
        public static Boolean In<T>(this T item, IEnumerable<T> collection)
        {
            return collection.Any(c => Equals(c, item));
        }

        public static Boolean ContainsFlag<TEnum>(this Enum item, TEnum checking)
            where TEnum : Enum, IConvertible
        {
            var l = Convert.ToInt64(item);
            var r = Convert.ToInt64(checking);
            return (l | r) == l;
        }

        public static Byte[] GetBytes(this String str)
            => TypeCore.GetBytes(str);
       

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

        public static Boolean Congruent<T>(this HashSet<T> left, HashSet<T> right)
        {
            if (right.Count != left.Count)
                return false;

            foreach (var item in left)
            {
                if (!right.Contains(item))
                    return false;
            }

            return true;
        }

        public static unsafe String GetString(this Byte[] tempByte, Int32 useBytes)
        {
            if (tempByte == null)
                return null;

            fixed (Byte* bptr = tempByte)
            {
                var cptr = (Char*) bptr;
                return new String(cptr, 0, useBytes / 2);
            }
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