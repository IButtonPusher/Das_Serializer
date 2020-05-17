using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Das.Serializer
{
    public class TypeCore : ITypeCore, IComparer<PropertyInfo>
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



        private static readonly ConcurrentDictionary<Type, Boolean> CollectionTypes;

        private static readonly ConcurrentDictionary<Type, Boolean> _typesKnownSettableProperties;
        private static readonly ConcurrentDictionary<Type, Type> _cachedGermane;

        private static readonly ConcurrentDictionary<Type, ConstructorInfo> CachedConstructors;

        private static readonly ConcurrentDictionary<Type, Boolean> Leaves;

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(Int32), typeof(Double), typeof(Decimal),
            typeof(Int64), typeof(Int16), typeof(SByte),
            typeof(Byte), typeof(UInt64), typeof(UInt16),
            typeof(UInt32), typeof(Single)
        };

        static TypeCore()
        {
            _cachedGermane = new ConcurrentDictionary<Type, Type>();
            CachedProperties = new ConcurrentDictionary<Type,
                IEnumerable<PropertyInfo>>();
            
            _typesKnownSettableProperties = new ConcurrentDictionary<Type, Boolean>();
            CachedConstructors = new ConcurrentDictionary<Type, ConstructorInfo>();
            CollectionTypes = new ConcurrentDictionary<Type, Boolean>();
            Leaves = new ConcurrentDictionary<Type, Boolean>();
        }

        private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>
            CachedProperties;

        private static readonly ConcurrentDictionary<Type, ISet<PropertyInfo>>
            CachedProperties2 = new ConcurrentDictionary<Type, ISet<PropertyInfo>>();

        public Boolean TryGetNullableType(Type candidate, out Type primitive)
        {
            primitive = null;
            if (!candidate.IsGenericType ||
                candidate.GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;

            primitive = candidate.GetGenericArguments()[0];
            return true;
        }

        public Type GetGermaneType(Type ownerType)
        {
            if (_cachedGermane.TryGetValue(ownerType, out var typ))
                return typ;

            try
            {
                if (!typeof(IEnumerable).IsAssignableFrom(ownerType))
                    return ownerType;

                if (ownerType.IsArray)
                {
                    typ = ownerType.GetElementType();
                    return typ;
                }

                if (typeof(IDictionary).IsAssignableFrom(ownerType))
                {
                    typ = GetKeyValuePair(ownerType);
                    if (typ != null)
                        return typ;
                }

                var gargs = ownerType.GetGenericArguments();

                switch (gargs.Length)
                {
                    case 1 when ownerType.IsGenericType:
                        typ = gargs[0];
                        return typ;
                    case 2:
                        var lastChanceDictionary = typeof(IDictionary<,>).MakeGenericType(gargs);
                        typ = lastChanceDictionary.IsAssignableFrom(ownerType)
                            ? GetKeyValuePair(lastChanceDictionary)
                            : ownerType;
                        return typ;
                    case 0:
                        var gen0 = ownerType.GetInterfaces().FirstOrDefault(i =>
                            i.IsGenericType);
                        return GetGermaneType(gen0);
                }
            }
            finally
            {
                if (typ != null)
                    _cachedGermane.TryAdd(ownerType, typ);
            }

            return null;
        }

        private static Type GetKeyValuePair(Type dicType)
        {
            var akas = dicType.GetInterfaces();
            for (var c = 0; c < akas.Length; c++)
            {
                var interf = akas[c];
                if (!interf.IsGenericType)
                    continue;

                var genericArgs = interf.GetGenericArguments();
                if (genericArgs.Length != 1 || !genericArgs[0].IsValueType)
                    continue;

                return genericArgs[0];
            }

            return null;
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
        {
            if (Leaves.TryGetValue(t, out var l))
                return l;

            return Leaves[t] = AmIALeaf(t, isStringCounts);
        }

        Boolean ITypeCore.IsLeaf(Type t, Boolean isStringCounts)  
            => IsLeaf(t, isStringCounts);

        // ReSharper disable once InconsistentNaming
        private static Boolean AmIALeaf(Type t, Boolean isStringCounts) 
            => (t.IsValueType || isStringCounts &&
            t == Const.StrType)
            && Type.GetTypeCode(t) > TypeCode.DBNull;
            

        public Boolean IsAbstract(PropertyInfo propInfo)
            => propInfo.GetGetMethod()?.IsAbstract == true ||
               propInfo.GetSetMethod()?.IsAbstract == true;

        [MethodImpl(256)]
        public Boolean IsCollection(Type type)
        {
            var val = CollectionTypes.GetOrAdd(type, (t) => 
                t != Const.StrType && typeof(IEnumerable).IsAssignableFrom(type));

            return val;
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


        protected static Boolean IsAnonymousType(Type type)
        {
            return type.Namespace == null &&
                   type.IsGenericType &&
                   type.IsSealed && type.BaseType == Const.ObjectType &&
                   Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.Public) == TypeAttributes.NotPublic;
        }

        public ISet<PropertyInfo> GetPublicProperties2(Type type, Boolean numericFirst = true)
        {

            if (CachedProperties2.TryGetValue(type, out var results))
                return results;

            if (numericFirst)
                results = new SortedSet<PropertyInfo>(this);
            else results = new HashSet<PropertyInfo>();

            //var res = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            //todo: remove this and get it to work with explicits
            if (type.IsInterface)
            {
                foreach (var parentInterface in type.GetInterfaces())
                {
                    var letsAdd = GetPublicProperties2(parentInterface, false);

                    foreach (var l in letsAdd)
                        results.Add(l);
                }
            }
            else
            {
                var bt = type.BaseType;

                while (bt != null)
                {
                    foreach (var pp in GetPublicProperties2(type.BaseType, false))
                    {
                        results.Add(pp);
                    }

                    bt = bt.BaseType;
                }
            }


            CachedProperties2.TryAdd(type, results);
            return results;

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

            var rar = numericFirst
                ? res.OrderByDescending(p => IsLeaf(p.PropertyType, false)).ToArray()
                : res.ToArray();

           // if (numericFirst)
           //     res = res.OrderByDescending(p => IsLeaf(p.PropertyType, false));

            //var rar = res.ToArray();

            CachedProperties.TryAdd(type, rar);

            foreach (var prop in rar)
                yield return prop;
        }

        public PropertyInfo FindPublicProperty(Type type, String propertyName)
            => GetPublicProperties(type, false).
                FirstOrDefault(p => p.Name == propertyName);
        
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

        public Int32 Compare(PropertyInfo x, PropertyInfo y)
        {
            if (ReferenceEquals(null, x) || ReferenceEquals(null, y))
                return Int32.MaxValue;
            return String.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }
}