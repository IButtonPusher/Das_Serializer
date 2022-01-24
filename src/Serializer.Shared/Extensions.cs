using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Extensions
{
    public static class ExtensionMethods
    {
        public static ISerializationCore SerializationCore
        {
            get => _dynamicFacade ?? (_dynamicFacade = new DefaultStateProvider());
            set => _dynamicFacade = value;
        }

        internal static ISerializerSettings Settings
        {
            get => _settings ?? (_settings = DasSettings.CloneDefault());
            set => _settings = value;
        }

        public static Boolean TryGetCustomAttribute<TEnum, TAttribute>(this TEnum val,
                                                                       out TAttribute attrib)
           where TEnum : Enum
           where TAttribute : Attribute
        {
           var vStr = val.ToString();
           var fi = typeof(TEnum).GetField(vStr);
           if (fi != null && fi.TryGetCustomAttribute(out attrib))
           {
              return true;
           }

           attrib = default!;
           return false;
        }

        public static Boolean TryGetCustomAttribute<TAttribute>(this MemberInfo m,
                                                                out TAttribute value)
           where TAttribute : Attribute
        {
           var attributes = m.GetCustomAttributes(typeof(TAttribute), true);
           if (attributes.Length == 0)
           {
              value = default!;
              return false;
           }

           value = (attributes[0] as TAttribute)!;
           return value != null;
        }

        public static Boolean AreCongruent<T>(this IReadOnlyList<T> left,
                                              IReadOnlyList<T> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);
            if (ReferenceEquals(null, right))
                return false;

            if (right.Count != left.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
                if (!Equals(left[i], right[i]))
                    return false;

            return true;
        }

        public static Boolean AreListsCongruent<T>(this IList<T> left,
                                                   IList<T> right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);
            if (ReferenceEquals(null, right))
                return false;

            if (right.Count != left.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
                if (!Equals(left[i], right[i]))
                    return false;

            return true;
        }


        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Single f1,
                                             Single f2)
        {
            return Math.Abs(f1 - f2) < 0.00001f;
        }

        [MethodImpl(256)]
        public static Boolean AreEqualEnough(this Int32 i1,
                                             Double d1)
        {
            return Convert.ToInt32(d1) == i1;
        }

        public static T BuildDefault<T>(this Type type)
        {
            return (T) type.BuildDefault()!;
        }

        public static Object? BuildDefault(this Type type)
        {
            return SerializationCore.ObjectInstantiator.BuildDefault(type, false);
        }

        public static Boolean Congruent<T>(this IList<T>? left,
                                           IList<T>? right)
        {
            if (ReferenceEquals(null, left))
                return ReferenceEquals(null, right);
            if (ReferenceEquals(null, right))
                return false;

            if (right.Count != left.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
                if (!Equals(left[i], right[i]))
                    return false;

            return true;
        }

        public static Boolean ContainsAll<T>(this IList<T> left,
                                             List<T> right)
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

        public static void ForEach<T>(this IEnumerable<T> source,
                                      Action<T> action,
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


        ///// <summary>
        /////     Creates an easier to read string depiction of a type name, particularly
        /////     with generics. Can be parsed back into a type using DasType.FromClearName(..)
        /////     into
        ///// </summary>
        ///// <param name="type"></param>
        ///// <param name="isOmitAssemblyName">
        /////     Guarantees that the output string will be
        /////     valid xml or json markup but may lead to slower deserialization
        ///// </param>
        //public static String GetClearName(this Type type,
        //                                  Boolean isOmitAssemblyName)
        //{
        //    return SerializationCore.TypeInferrer.ToClearName(type, isOmitAssemblyName);
        //}

        [MethodImpl(256)]
        public static String GetConsumingString(this StringBuilder sb)
        {
            var res = sb.ToString();
            sb.Length = 0;
            return res;
        }

       

      

        public static Encoding GetEncoding(this Byte[] bom)
        {
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }


        public static FieldInfo GetInstanceFieldOrDie(this Type classType,
                                                      String fieldName)
        {
            return classType.GetField(fieldName, Const.NonPublic)
                   ?? DieBart(classType, fieldName);
        }

        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case PropertyInfo pi:
                    return pi.PropertyType;

                case FieldInfo fi:
                    return fi.FieldType;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static PropertyInfo GetStaticPropertyOrDie(this Type classType,
                                                          String propertyName)
        {
            var wot = classType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            //if (wot == null && classType.IsInterface)
            //    foreach (var @interface in classType.GetInterfaces())
            //    {
            //        wot = @interface.GetProperty(propertyName);
            //        if (wot != null)
            //            break;
            //    }

            return wot ?? throw new MissingMemberException(classType.FullName, propertyName);
        }

      
        public static MemberInfo[] GetMembersOrDie(this Type classType,
                                                   String memberName)
        {
            return classType.GetMembersOrDie(memberName, Const.PublicInstance);
        }

        public static MemberInfo[] GetMembersOrDie(this Type classType,
                                                    String memberName,
                                                    BindingFlags flags)
        {
            var wot = classType.GetMember(memberName);
            if (wot == null && classType.IsInterface)
                foreach (var @interface in classType.GetInterfaces())
                {
                    wot = @interface.GetMember(memberName, flags);
                    if (wot != null)
                        break;
                }

            return wot ?? throw new MissingMemberException(classType.FullName, memberName);
        }


        public static Boolean AreEqual(this ISerializationDepth left,
                                       ISerializationDepth? right)
        {
            if (ReferenceEquals(null, right))
                return false;

            return left.SerializationDepth == right.SerializationDepth &&
                   left.IsOmitDefaultValues == right.IsOmitDefaultValues &&
                   left.IsRespectXmlIgnore == right.IsRespectXmlIgnore;
        }


        public static T GetPropertyValue<T>(this Object obj,
                                            String propertyName)
        {
            return SerializationCore.ObjectManipulator.GetPropertyValue<T>(obj, propertyName);
        }

        public static TProperty GetPropertyValue<TObject, TProperty>(this TObject obj,
                                                                     String propertyName)
        {
            return SerializationCore.ObjectManipulator.GetPropertyValue<TObject, TProperty>(
                obj, propertyName);
        }

        public static Object? GetPropertyValue(this Object obj,
                                               String propertyName)
        {
            return SerializationCore.ObjectManipulator.GetPropertyValue(obj, propertyName,
                PropertyNameFormat.Default);
        }

       

        public static FieldInfo GetPrivateStaticFieldOrDie(this Type classType,
                                                           String fieldName)
        {
            return classType.GetField(fieldName, Const.PrivateStatic)
                   ?? DieBart(classType, fieldName);
        }

      

        public static MethodInfo GetterOrDie(this Type tType,
                                             String property,
                                             out PropertyInfo propertyInfo,
                                             BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            propertyInfo = tType.GetProperty(property, flags)!;
            if (propertyInfo != null)
            {
                var method = propertyInfo.GetGetMethod();
                if (method != null)
                    return method;
            }

            return tType.GetProperty(property, flags)?.GetGetMethod() ??
                   throw new InvalidOperationException(tType.Name + "." + property);
        }


        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item,
                                      T c1,
                                      T c2,
                                      T c3,
                                      T c4,
                                      T c5,
                                      T c6,
                                      T c7)
        {
            return Equals(item, c7) || item.IsIn(c1, c2, c3, c4, c5, c6);
        }

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item,
                                      T c1,
                                      T c2,
                                      T c3,
                                      T c4,
                                      T c5,
                                      T c6)
        {
            return Equals(item, c6) || item.IsIn(c1, c2, c3, c4, c5);
        }

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item,
                                      T c1,
                                      T c2,
                                      T c3,
                                      T c4,
                                      T c5)
        {
            return Equals(item, c5) || item.IsIn(c1, c2, c3, c4);
        }

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item,
                                      T c1,
                                      T c2,
                                      T c3,
                                      T c4)
        {
            return Equals(item, c4) || item.IsIn(c1, c2, c3);
        }

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item,
                                      T c1,
                                      T c2,
                                      T c3)
        {
            return Equals(item, c3) || item.IsIn(c1, c2);
        }

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item,
                                      T c1,
                                      T c2)
        {
            return Equals(item, c1) || Equals(item, c2);
        }

        [MethodImpl(256)]
        public static Boolean IsIn<T>(this T item,
                                      IEnumerable<T> collection)
        {
            return collection.Any(c => Equals(c, item));
        }


        public static Boolean IsString(this Type t)
        {
            return t == typeof(String);
        }

        public static MethodInfo SetterOrDie(this Type type,
                                             String property,
                                             BindingFlags flags =
                                                 BindingFlags.Public | BindingFlags.Instance)
        {
            return type.GetProperty(property, flags)?.GetSetMethod(true) ??
                   throw new InvalidOperationException();
        }


        [MethodImpl(256)]
        public static Boolean IsCloseToZero(this Double d)
        {
            return Math.Abs(d) < MyEpsilon;
        }

        public static T[] Take<T>(this T[] arr,
                                  Int32 startIndex,
                                  Int32 length)
        {
            var res = new T[length];
            if (typeof(T).IsPrimitive)
                Buffer.BlockCopy(arr, startIndex, res, 0, length);
            else
                for (var i = 0; i < length; i++)
                    res[i] = arr[i];

            return res;
        }


        public static String ToString<T>(this IList<T> list,
                                         Char sep,
                                         Char? excludeWhenFirst = null)
        {
            if (list == null || list.Count == 0)
                return String.Empty;

            if (list.Count == 1)
                return list[0]?.ToString() ?? String.Empty;

            var sb = new StringBuilder();
            foreach (var i in list)
            {
                if (i == null)
                    continue;

                if (excludeWhenFirst != null && i.ToString()?[0] == excludeWhenFirst)
                    sb.Append($"{sep}");
                else
                    sb.Append($"{i}{sep}");
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public static String ToString<T>(this IEnumerable<T> list,
                                         Char sep,
                                         Char? excludeWhenFirst = null)
        {
            if (list == null)
                return String.Empty;

            var sb = new StringBuilder();
            foreach (var i in list)
            {
                if (null == i)
                    continue;

                if (excludeWhenFirst != null && i.ToString()?[0] == excludeWhenFirst)
                    sb.Append($"{sep}");
                else
                    sb.Append($"{i}{sep}");
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public static Boolean TryGetMethod(this Type classType,
                                           String methodName,
                                           out MethodInfo method,
                                           params Type[] parameters)
        {
            return classType.TryGetMethod(methodName, out method,
                BindingFlags.Instance | BindingFlags.Public, parameters);
        }

        public static Boolean TryGetMethod(this Type classType,
                                           String methodName,
                                           out MethodInfo method,
                                           BindingFlags flags = BindingFlags.Instance | BindingFlags.Public,
                                           params Type[] parameters)
        {
            method = parameters.Length > 0
                ? classType.GetMethod(methodName, flags, null, parameters, null)!
                : classType.GetMethod(methodName, flags)!;

            return method != null;
        }

        public static Boolean TryGetPropertyValue<T>(this Object obj,
                                                     String propertyName,
                                                     out T result)
        {
            return SerializationCore.ObjectManipulator.TryGetPropertyValue(obj,
                propertyName, out result);
        }

        public static Boolean TryGetPropertyValue(this Object obj,
                                                  String propertyName,
                                                  out Object result)
        {
            return SerializationCore.ObjectManipulator.TryGetPropertyValue(obj,
                propertyName, out result);
        }

        //public static Boolean TrySetPropertyValue(this Object obj,
        //                                          String propertyName,
        //                                          Object value)
        //{
        //    return SerializationCore.ObjectManipulator.TrySetProperty(obj.GetType(), propertyName,
        //        ref obj, value);
        //}

        

        private static FieldInfo DieBart(Type classType,
                                         String fieldName)
        {
            throw new InvalidOperationException(classType.Name + "." + fieldName);
        }

        private static ISerializationCore? _dynamicFacade;

        private static ISerializerSettings? _settings;
        private const Double MyEpsilon = 0.00001;
    }
}
