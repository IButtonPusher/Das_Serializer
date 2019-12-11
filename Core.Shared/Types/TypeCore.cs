using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Das.Serializer;

namespace Serializer.Core
{
    public class TypeCore : ITypeCore
    {
        private ISerializerSettings _settings;
        

        public virtual ISerializerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }

        public TypeCore(ISerializerSettings settings)
        {
            _settings = settings;
        }



        private static readonly ConcurrentDictionary<Type, Int32> CollectionTypes;

        private static readonly ConcurrentDictionary<Type, Boolean> _typesKnownSettableProperties;

        private static readonly ConcurrentDictionary<Type, ConstructorInfo> CachedConstructors;

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(Int32), typeof(Double), typeof(Decimal),
            typeof(Int64), typeof(Int16), typeof(SByte),
            typeof(Byte), typeof(UInt64), typeof(UInt16),
            typeof(UInt32), typeof(Single)
        };

        static TypeCore()
        {
            CachedProperties = new ConcurrentDictionary<Type,
                IEnumerable<PropertyInfo>>();
            _typesKnownSettableProperties = new ConcurrentDictionary<Type, Boolean>();
            CachedConstructors = new ConcurrentDictionary<Type, ConstructorInfo>();
            CollectionTypes = new ConcurrentDictionary<Type, Int32>();
        }

        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>
            CachedProperties;

        public Boolean TryGetNullableType(Type candidate, out Type primitive)
        {
            primitive = null;
            if (!candidate.IsGenericType ||
                candidate.GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;

            primitive = candidate.GetGenericArguments()[0];
            return true;
        }

        public virtual Boolean HasSettableProperties(Type type)
        {
            if (_typesKnownSettableProperties.TryGetValue(type, out var yesOrNo))
                return yesOrNo;

            var allPublic = GetPublicProperties(type, false);
            yesOrNo = allPublic.Any(p => p.CanWrite);
            _typesKnownSettableProperties.TryAdd(type, yesOrNo);
            return yesOrNo;
        }

        public static Boolean IsLeaf(Type t, Boolean isStringCounts)
            => (t.IsValueType || isStringCounts && t == Const.StrType)
                         && Type.GetTypeCode(t) > TypeCode.DBNull;

        Boolean ITypeCore.IsLeaf(Type t, Boolean isStringCounts) => IsLeaf(t, isStringCounts);

        public Boolean IsAbstract(PropertyInfo propInfo)
            => propInfo.GetGetMethod()?.IsAbstract == true ||
               propInfo.GetSetMethod()?.IsAbstract == true;

        public Boolean IsCollection(Type type)
        {
            if (type == null)
                return false;

            if (!CollectionTypes.TryGetValue(type, out var val))
            {
                val = typeof(IEnumerable).IsAssignableFrom(type) && type != Const.StrType
                    ? 1
                    : 0;
                CollectionTypes[type] = val;
            }

            return val == 1;
        }
       

        public Boolean IsUseless(Type t) => t == null || t == Const.ObjectType;

        public Boolean IsNumeric(Type myType) => NumericTypes.Contains(
            Nullable.GetUnderlyingType(myType) ?? myType);

        public Boolean HasEmptyConstructor(Type t)
            => t.GetConstructor(Type.EmptyTypes) != null;

        public Boolean IsInstantiable(Type t) =>
            !IsUseless(t) && !t.IsAbstract && !t.IsInterface;

        protected static Boolean IsString(Type t) => t == Const.StrType;

        public static Decimal ToDecimal(Byte[] bytes)
        {
            var bits = new Int32[4];
            for (var i = 0; i <= 15; i += 4)
                bits[i / 4] = BitConverter.ToInt32(bytes, i);

            return new Decimal(bits);
        }

//        public static unsafe Byte[] GetBytes(String str)
//        {
//            var len = str.Length * 2;
//            var bytes = new Byte[len];
//            fixed (void* ptr = str)
//            {
//                Marshal.Copy(new IntPtr(ptr), bytes, 0, len);
//            }
//
//            return bytes;
//        }
//
//        public static Byte[] GetBytes(Decimal dec)
//        {
//            var bits = Decimal.GetBits(dec);
//            var bytes = new List<Byte>();
//
//            foreach (var i in bits)
//                bytes.AddRange(BitConverter.GetBytes(i));
//
//
//            return bytes.ToArray();
//        }

        protected static Boolean IsAnonymousType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.IsGenericType &&
                   type.Namespace == null &&
                   type.IsSealed && type.BaseType == Const.ObjectType &&
                   Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.Public) == TypeAttributes.NotPublic;
        }

        public IEnumerable<PropertyInfo> GetPublicProperties(Type type, Boolean numericFirst = true)
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
        
        public Boolean TryGetPropertiesConstructor(Type type, out ConstructorInfo constr)
        {
            constr = null;
            var isAnomymous = IsAnonymousType(type);

            if (!isAnomymous && CachedConstructors.TryGetValue(type, out constr))
                return constr != null;

            var rProps = new Dictionary<String, Type>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var p in type.GetProperties().Where(p => !p.CanWrite && p.CanRead))
                rProps.Add(p.Name, p.PropertyType);
            foreach (var con in type.GetConstructors())
            {
                if (con.GetParameters().Length <= 0 || !con.GetParameters().All(p =>
                        rProps.ContainsKey(p.Name) && rProps[p.Name] == p.ParameterType))
                    continue;

                constr = con;
                break;
            }

            if (constr == null)
                return false;

            if (isAnomymous)
                return true;


            CachedConstructors.TryAdd(type, constr);
            return true;
        }

    }
}