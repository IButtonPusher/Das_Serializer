using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Das.Serializer;

// ReSharper disable UnusedMember.Global

namespace Das.Extensions
{
    public static class CoreExtensionMethods
    {
        //internal static ISerializerSettings Settings { get; set; }

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


        public static String ToString<T>(this IList<T> list, Char sep,
            Char? excludeWhenFirst = null)
        {
            if (list == null || list.Count == 0)
                return String.Empty;

            if (list.Count == 1)
                return list[0]!.ToString();

            var sb = new StringBuilder();
            foreach (var i in list)
            {
                if (null == i)
                    continue;

                if (excludeWhenFirst != null && i.ToString()[0] == excludeWhenFirst)
                    sb.Append($"{sep}");
                else
                    sb.Append($"{i}{sep}");
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public static String ToString<T>(this IEnumerable<T> list, Char sep,
            Char? excludeWhenFirst = null)
        {
            if (list == null)
                return String.Empty;

            var sb = new StringBuilder();
            foreach (var i in list)
            {
                if (null == i)
                    continue;

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
        public static Boolean IsIn<T>(this T item, T c1, T c2, T c3, 
            T c4, T c5, T c6, T c7) =>
            Equals(item, c7) || item.IsIn(c1, c2, c3, c4, c5, c6);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2, 
            T c3, T c4, T c5, T c6) =>
            Equals(item, c6) || item.IsIn(c1, c2, c3, c4, c5);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2, 
            T c3, T c4, T c5) =>
            Equals(item, c5) || item.IsIn(c1, c2, c3, c4);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, 
            T c2, T c3, T c4)
            => Equals(item, c4) || item.IsIn(c1, c2, c3);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, 
            T c2, T c3) =>
            Equals(item, c3) || item.IsIn(c1, c2);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, T c1, T c2)
            => Equals(item, c1) || Equals(item, c2);

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item, IEnumerable<T> collection)
        {
            return collection.Any(c => Equals(c, item));
        }

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Single f1, Single f2)
            => Math.Abs(f1 - f2) < 0.00001f;

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Int32 i1, Double d1)
            => Convert.ToInt32(d1) == i1;

        public static Boolean Congruent<T>(this IList<T> left, IList<T> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);
            if (ReferenceEquals(null, right))
                return false;

            if (right.Count != left.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (!Equals(left[i], right[i]))
                    return false;
            }

            return true;
        }

        public static Boolean AreCongruent<T>(this IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);
            if (ReferenceEquals(null, right))
                return false;

            if (right.Count != left.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (!Equals(left[i], right[i]))
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

        public static MethodInfo GetterOrDie(this Type tType, String property,
            out PropertyInfo propertyInfo,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            propertyInfo = tType.GetProperty(property, flags);
            if (propertyInfo != null)
            {
                var method = propertyInfo.GetGetMethod();
                if (method != null)
                    return method;
            }

            return tType.GetProperty(property, flags)?.GetGetMethod() ??
                   throw new InvalidOperationException(tType.Name + "." + property);
        }

        public static MethodInfo SetterOrDie(this Type type, String property, BindingFlags flags =
            BindingFlags.Public | BindingFlags.Instance)
        {
            return type.GetProperty(property, flags)?.GetSetMethod(true) ??
                   throw new InvalidOperationException();
        }

        public static Boolean TryGetMethod(this Type classType, String methodName,
            out MethodInfo method,
            params Type[] parameters) => classType.TryGetMethod(methodName, out method,
            BindingFlags.Instance | BindingFlags.Public, parameters);

        public static Boolean TryGetMethod(this Type classType, String methodName,
            out MethodInfo method,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public,
            params Type[] parameters)
        {
            method = parameters.Length > 0
                ? classType.GetMethod(methodName, flags, null, parameters, null)
                : classType.GetMethod(methodName, flags);

            return method != null;
        }

        public static MethodInfo GetMethodOrDie(this Type classType, String methodName)
        {
            var wot = classType.GetMethod(methodName, Type.EmptyTypes) ?? 
                      classType.GetMethod(methodName);
            if (wot == null && classType.IsInterface)
            {
                foreach (var @interface in classType.GetInterfaces())
                {
                    wot = @interface.GetMethod(methodName);
                    if (wot != null)
                        break;
                }

                //wot = classType.GetMethod(methodName, BindingFlags.FlattenHierarchy
                //| BindingFlags.NonPublic);
            }

            return wot ?? throw new MissingMethodException(classType.Name, methodName);
        }

        public static ConstructorInfo GetDefaultConstructorOrDie(this Type classType)
            => classType.GetConstructor(BindingFlags.Instance | BindingFlags.Public
                                                              | BindingFlags.NonPublic,
                   null, Type.EmptyTypes, null) ?? throw new MissingMethodException(
                   classType.FullName, "ctor");


        public static FieldInfo GetInstanceFieldOrDie(
            this Type classType,
            String fieldName) => classType.GetField(fieldName, Const.NonPublic) 
                                 ?? DieBart(classType, fieldName);

        public static FieldInfo GetStaticFieldOrDie(
            this Type classType,
            String fieldName) => classType.GetField(fieldName, Const.PrivateStatic) 
                                 ?? DieBart(classType, fieldName);

        public static MethodInfo GetMethodOrDie(
            this Type classType, 
            String methodName,
            BindingFlags flags) 
                
            => classType.GetMethod(methodName, flags) ?? Die(classType, methodName);

        public static MethodInfo GetMethodOrDie(
            this Type classType, 
            String methodName,
            BindingFlags flags, 
            Type[] parameters)
                
            => classType.GetMethod(methodName, flags, null, 
                   parameters, null)
                    ?? Die(classType, methodName);

        public static MethodInfo GetMethodOrDie(
            this Type classType, 
            String methodName,
            BindingFlags flags, 
            Type p1)
                
            => classType.GetMethod(methodName, flags, null, 
                   new []{p1}, null)
                    ?? Die(classType, methodName);

        public static MethodInfo GetPublicStaticMethodOrDie(
            this Type classType,
            String methodName)
        {
            return classType.GetMethod(methodName, Const.PublicStatic,
                       null, Type.EmptyTypes, null)
                   ?? classType.GetMethod(methodName, Const.PublicStatic)
                   ?? Die(classType, methodName);
        }

        public static MethodInfo GetPublicStaticMethodOrDie(
            this Type classType,
            String methodName,
            params Type[] prms)

        {
            return classType.GetMethod(methodName, Const.PublicStatic,
                       null, prms, null)
                   ?? Die(classType, methodName);
        }

        //public static MethodInfo GetPublicStaticMethodOrDie(
        //    this Type classType,
        //    String methodName,
        //    Type p1,
        //    Type p2)

        //{
        //    return classType.GetMethod(methodName, Const.PublicStatic,
        //               null, new[] {p1, p2}, null)
        //           ?? Die(classType, methodName);
        //}

        public static MethodInfo GetMethodOrDie(
            this Type classType,
            String methodName,
            BindingFlags flags,
            Type p1,
            Type p2)

            => classType.GetMethod(methodName, flags, null,
                   new[] {p1, p2}, null)
               ?? Die(classType, methodName);

        public static MethodInfo GetMethodOrDie(
            this Type classType,
            String methodName,
            Type[] parameters)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, parameters);
        }

        public static MethodInfo GetMethodOrDie(
            this Type classType,
            String methodName,
            Type p1)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, new []{p1});
        }

        public static MethodInfo GetMethodOrDie(
            this Type classType,
            String methodName,
            Type p1,
            Type p2)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, new []{p1, p2});
        }

        public static MethodInfo GetMethodOrDie(
            this Type classType,
            String methodName,
            Type p1,
            Type p2,
            Type p3)
        {
            return GetMethodOrDie(classType, methodName,
                BindingFlags.Instance | BindingFlags.Public, 
                new []{p1, p2, p3});
        }

        private static MethodInfo Die(Type classType, String methodName)
        {
            throw new InvalidOperationException(classType.Name + "->" + methodName);
        }

        private static FieldInfo DieBart(Type classType, String fieldName)
        {
            throw new InvalidOperationException(classType.Name + "." + fieldName);
        }
    }
}