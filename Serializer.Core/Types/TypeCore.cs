using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Das.CoreExtensions;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class TypeCore : ITypeCore
    {
        private ISerializerSettings _settings;
        public virtual ISerializerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }
        

        protected TypeCore(ISerializerSettings settings)
        {
            _settings = settings;
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float)
        };

        static TypeCore()
        {
            CachedProperties = new ConcurrentDictionary<Type,
                IEnumerable<PropertyInfo>>();
        }

        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>
            CachedProperties;

        public bool TryGetNullableType(Type candidate, out Type primitive)
        {
            primitive = null;
            if (!candidate.IsGenericType || 
                candidate.GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;

            primitive = candidate.GetGenericArguments()[0];
            return true;
        }

        public bool IsLeaf(Type t, bool isStringCounts)
            => t != null && (t.IsValueType || isStringCounts && t == Const.StrType)
                         && Type.GetTypeCode(t) > TypeCode.DBNull;

        public bool IsAbstract(PropertyInfo propInfo)
            => propInfo.GetGetMethod()?.IsAbstract == true ||
               propInfo.GetSetMethod()?.IsAbstract == true;

        public bool IsCollection(Type type) 
            => type != null &&
                typeof(IEnumerable).IsAssignableFrom(type) && type != Const.StrType;

        public bool IsUseless(Type t) => t == null || t == typeof(Object);

        public bool IsNumeric(Type myType) => NumericTypes.Contains(
            Nullable.GetUnderlyingType(myType) ?? myType);

        public bool HasEmptyConstructor(Type t)
            => t.GetConstructor(Type.EmptyTypes) != null;

        public bool IsInstantiable(Type t) =>
            !IsUseless(t) && !t.IsAbstract && !t.IsInterface;

        public static Boolean IsString(Type t) => t == Const.StrType;

        public static decimal ToDecimal(byte[] bytes)
        {
            var bits = new Int32[4];
            for (var i = 0; i <= 15; i += 4)
                bits[i / 4] = BitConverter.ToInt32(bytes, i);

            return new decimal(bits);
        }

        public static unsafe Byte[] GetBytes(String str)
        {
            var len = str.Length * 2;
            var bytes = new Byte[len];
            fixed (void* ptr = str)
            {
                Marshal.Copy(new IntPtr(ptr), bytes, 0, len);
            }

            return bytes;
        }

        public static byte[] GetBytes(decimal dec)
        {
            var bits = Decimal.GetBits(dec);
            var bytes = new List<byte>();

            foreach (var i in bits)
                bytes.AddRange(BitConverter.GetBytes(i));


            return bytes.ToArray();
        }

        public static bool IsAnonymousType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.IsGenericType && 
                type.Namespace == null && 
                type.IsSealed && type.BaseType == Const.ObjectType &&
                Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && type.Attributes.ContainsFlag(TypeAttributes.NotPublic);
        }

        public IEnumerable<PropertyInfo> GetPublicProperties(Type type, bool numericFirst = true)
        {
            if (CachedProperties.TryGetValue(type, out var res))
            {
                foreach (var p in res)
                    yield return p;

                yield break;
            }

            res = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var byName = new HashSet<String>(res.Select(p => p.Name));

            //todo: remove this and get it to work with explicits
            if (type.IsInterface)
            {
                var results = new HashSet<PropertyInfo>(res);

                foreach (var parentInterface in type.GetInterfaces())
                {
                    foreach (var pp in GetPublicProperties(parentInterface, false))
                    {
                        if (byName.Add(pp.Name))
                            results.Add(pp);
                    }
                }

                res = results;
            }
            else
            {
                var bt = type.BaseType;

                while (bt != null)
                {
                    var results = new HashSet<PropertyInfo>(res);

                    foreach (var pp in GetPublicProperties(type.BaseType, false))
                    {
                        if (byName.Add(pp.Name))
                            results.Add(pp);
                    }

                    bt = bt.BaseType;
                }
            }
            if (numericFirst)
                res = res.OrderByDescending(p => IsLeaf(p.PropertyType, false));

            CachedProperties.TryAdd(type, res);

            foreach (var prop in res)
                yield return prop;
        }

        
    }
}
