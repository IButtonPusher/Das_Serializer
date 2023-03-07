using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks; 

namespace Das.Serializer
{
    public class TypeCore : ITypeCore,
                            IComparer<PropertyInfo>
    {
        static TypeCore()
        {
            _typeConverters = new ConcurrentDictionary<Type, TypeConverter>();
            _cachedGermane = new ConcurrentDictionary<Type, Type>();
            //CachedProperties = new ConcurrentDictionary<Type, ICollection<PropertyInfo>>();
            CachedProperties = new ConcurrentDictionary<Type, Lazy<ICollection<PropertyInfo>>>();

            _typesKnownSettableProperties = new ConcurrentDictionary<Type, Boolean>();
            CachedConstructors = new ConcurrentDictionary<Type, ConstructorInfo?>();
            CollectionTypes = new ConcurrentDictionary<Type, Boolean>();
            LeavesNotString = new ConcurrentDictionary<Type, Boolean>();
            LeavesYesString = new ConcurrentDictionary<Type, bool>();
        }

        public TypeCore(ISerializerSettings settings)
        {
            _settings = settings;
        }

        public Int32 Compare(PropertyInfo? x,
                             PropertyInfo? y)
        {
            if (ReferenceEquals(null, x) || ReferenceEquals(null, y))
                return Int32.MaxValue;
            return String.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        public virtual ISerializerSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }


        public Boolean TryGetNullableType(Type candidate,
                                          out Type primitive)
        {
            return TryGetNullableTypeImpl(candidate, out primitive);
        }


        public Type GetGermaneType(Type ownerType)
        {
            return _cachedGermane.GetOrAdd(ownerType, GetGermaneTypeImpl);
        }

        private Type GetGermaneTypeImpl(Type ownerType)
        {
            Type? typ = null;

            if (ownerType.IsArray)
            {
                typ = ownerType.GetElementType() ?? throw new InvalidOperationException();
                return typ;
            }

            if (!typeof(IEnumerable).IsAssignableFrom(ownerType) || ownerType == typeof(String))
                return ownerType;


            if (typeof(IDictionary).IsAssignableFrom(ownerType))
            {
                typ = GetKeyValuePair(ownerType)!;
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
                        ? GetKeyValuePair(lastChanceDictionary)!
                        : ownerType;
                    return typ;

                case 0:
                    var gen0 = ownerType.GetInterfaces().FirstOrDefault(i =>
                        i.IsGenericType);


                    return gen0 != null ? GetGermaneType(gen0) : ownerType;
            }

            return typ ?? throw new InvalidOperationException("Cannot load ");
        }


        public virtual Boolean HasSettableProperties(Type type)
        {
            if (_typesKnownSettableProperties.TryGetValue(type, out var yesOrNo))
                return yesOrNo;

            yesOrNo = false;

            foreach (var pubProp in GetPublicProperties(type, false))
            {
                if (pubProp.CanWrite)
                {
                    yesOrNo = true;
                    break;
                }
            }

            _typesKnownSettableProperties.TryAdd(type, yesOrNo);
            return yesOrNo;
        }

        Boolean ITypeCore.IsLeaf(Type t,
                                 Boolean isStringCounts)
        {
            return IsLeaf(t, isStringCounts);
        }


        public Boolean IsAbstract(PropertyInfo propInfo)
        {
            return propInfo.GetGetMethod()?.IsAbstract == true ||
                   propInfo.GetSetMethod()?.IsAbstract == true;
        }

        [MethodImpl(256)]
        public Boolean IsCollection(Type type)
        {
            return CollectionTypes.GetOrAdd(type, t =>
                t != Const.StrType && (typeof(IEnumerable).IsAssignableFrom(t) || 
                                       t.IsArray));
        }


        public Boolean IsUseless(Type? t)
        {
            return t == null || t == Const.ObjectType;
        }

        public Boolean IsNumeric(Type myType)
        {
            return NumericTypes.Contains(
                Nullable.GetUnderlyingType(myType) ?? myType);
        }


        public Boolean HasEmptyConstructor(Type t)
        {
            return t.GetConstructor(Const.AnyInstance, null, Type.EmptyTypes, null) != null;
        }

        public bool TryGetEmptyConstructor(Type t,
                                           out ConstructorInfo ctor)
        {
            ctor = t.GetConstructor(Const.AnyInstance, null, Type.EmptyTypes, null)!;
            return ctor != null;
        }

        public Boolean IsInstantiable(Type? t)
        {
            return !IsUseless(t) && !t!.IsAbstract && !t.IsInterface;
        }


        IEnumerable<PropertyInfo> ITypeCore.GetPublicProperties(Type type)
           => GetPublicProperties(type);

        IEnumerable<PropertyInfo> ITypeCore.GetPublicProperties(Type type,
                                                                Boolean numericFirst)
           => GetPublicProperties(type, numericFirst);


        public static IEnumerable<PropertyInfo> GetPublicProperties(Type type,
                                                             Boolean numericFirst = true)
        {
            var props = CachedProperties.GetOrAdd(type, GetPublicPropertiesLazy).Value;

            var rar = numericFirst
                ? props.OrderByDescending(p => IsLeaf(p.PropertyType, false)).ToArray()
                : props.ToArray();

            return rar;
        }

        private static Lazy<ICollection<PropertyInfo>> GetPublicPropertiesLazy(Type type)
        {
            return new Lazy<ICollection<PropertyInfo>>(() => GetPublicPropertiesImpl(type));
        }

        private static ICollection<PropertyInfo> GetPublicPropertiesImpl(Type type)
        {
            var lookup = new Dictionary<String, PropertyInfo>();

            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                lookup[prop.Name] = prop;
            }

            //todo: remove this and get it to work with explicits
            if (type.IsInterface)
            {
                foreach (var parentInterface in type.GetInterfaces())
                foreach (var pp in GetPublicProperties(parentInterface, false))
                {
                        if (!lookup.ContainsKey(pp.Name))
                            lookup.Add(pp.Name, pp);
                }
            }
            else
            {
                var bt = type.BaseType;

                while (bt != null)
                {
                    foreach (var pp in GetPublicProperties(bt, false))
                    {
                        if (!lookup.ContainsKey(pp.Name))
                            lookup.Add(pp.Name, pp);
                    }

                    bt = bt.BaseType;
                }
            }

            return lookup.Values;
        }


        public object? ConvertTo(Object obj,
                                 Type type)
        {
            var conv = GetTypeConverter(type);
            return conv.ConvertFrom(obj);
        }

        public bool CanChangeType(Type from,
                                  Type to)
        {
            var conv = GetTypeConverter(to);
            return conv.CanConvertFrom(from);
        }

        public string ConvertToInvariantString(Object obj)
        {
            var converter = GetTypeConverter(obj.GetType());
            return converter.ConvertToInvariantString(obj)!;
        }

        public object ConvertFromInvariantString(String str,
                                                 Type toType)
        {
            var conv = GetTypeConverter(toType);
            return conv.ConvertFromInvariantString(str)!;
        }

        public PropertyInfo? FindPublicProperty(Type type,
                                                String propertyName)
        {
            foreach (var prop in GetPublicProperties(type, false))
            {
                if (prop.Name == propertyName)
                    return prop;
            }

            return default;

            
        }

        public Boolean TryGetPropertiesConstructor(Type type,
                                                   out ConstructorInfo constr)
        {
            constr = null!;
            var isAnomymous = IsAnonymousType(type);

            constr = !isAnomymous
                ? CachedConstructors.GetOrAdd(type, GetPropertiesCtorImpl)!
                : GetPropertiesCtorImpl(type)!;
            return constr != null;
        }

        public string ChangePropertyNameFormat(String str,
                                               PropertyNameFormat newFormat)
        {
            switch (newFormat)
            {
                case PropertyNameFormat.Default:
                    return str;

                case PropertyNameFormat.PascalCase:
                    return ToPascalCase(str);

                case PropertyNameFormat.CamelCase:
                    return ToCamelCase(str);

                case PropertyNameFormat.SnakeCase:
                    return ToSnakeCase(str);

                default:
                    throw new ArgumentOutOfRangeException(nameof(newFormat), newFormat, null);
            }

            
        }

        

        /// <summary>
        ///     Returns the name in PascalCase
        /// </summary>
        public static String ToPascalCase(String name)
        {
            switch (name.Length)
            {
                case 0:
                    return name;

                case 1:
                    return name.ToUpper();
            }

            var res = new StringBuilder();

            var c = 0;
            for (; c < name.Length; c++)
            {
                if (name[c] == '_')
                    continue;

                res.Append(Char.ToUpper(name[c++]));
                break;
            }

            for (; c < name.Length; c++)
                if (name[c] == '_')
                {
                    if (c < name.Length - 1)
                        res.Append(Char.ToUpper(name[++c]));
                }
                else
                    res.Append(name[c]);


            return res.ToString();
        }

        public static String ToCamelCase(String name)
        {
            switch (name.Length)
            {
                case 0:
                    return name;

                case 1:
                    return name.ToLower();
            }

            var res = new StringBuilder();

            var c = 0;
            for (; c < name.Length; c++)
            {
                if (name[c] == '_')
                    continue;

                res.Append(char.ToLower(name[c++]));
                break;
            }

            for (; c < name.Length; c++)
                if (name[c] == '_')
                {
                    if (c < name.Length - 1)
                        res.Append(char.ToUpper(name[++c]));
                }
                else
                    res.Append(name[c]);


            return res.ToString();
        }

        public static String ToSnakeCase(String name)
        {
            switch (name.Length)
            {
                case 0:
                    return name;

                case 1:
                    return name.ToLower();
            }

            var res = new StringBuilder();

            var c = 0;
            for (; c < name.Length; c++)
            {
                if (name[c] == '_')
                    continue;

                res.Append(char.ToLower(name[c++]));
                break;
            }

            for (; c < name.Length; c++)
                if (char.IsUpper(name[c]))
                {
                    res.Append('_');
                    res.Append(char.ToLower(name[c]));
                }
                else
                    res.Append(char.ToLower(name[c]));

            return res.ToString();
        }

        public static Boolean IsLeaf(Type t,
                                     Boolean isStringCounts)
        {
            if (isStringCounts)
                return IsLeafImpl(t, true, LeavesYesString);

            return IsLeafImpl(t, false, LeavesNotString);
        }

       


        public static Boolean IsAnonymousType(Type type)
        {
            return type.Namespace == null &&
                   type.IsGenericType &&
                   type.IsSealed && type.BaseType == Const.ObjectType &&
                   Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                   && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                   && (type.Attributes & TypeAttributes.Public) == TypeAttributes.NotPublic;
        }

        protected static Boolean IsString(Type t)
        {
            return t == Const.StrType;
        }

        // ReSharper disable once InconsistentNaming
        private static Boolean AmIALeaf(Type t,
                                        Boolean isStringCounts)
        {
            return // value type or a string if desired
                (t.IsValueType || isStringCounts && t == Const.StrType)
                // not an object unless it's a nullable
                && Type.GetTypeCode(t) > TypeCode.DBNull;
        }

        private static Type? GetKeyValuePair(Type dicType)
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

        private static ConstructorInfo? GetPropertiesCtorImpl(Type type)
        {
            _rProps ??= new Dictionary<String, Type>(
                StringComparer.OrdinalIgnoreCase);
            var isDynamicType = type.Assembly.IsDynamic;

            foreach (var p in type.GetProperties())
            {
                if (!p.CanRead)
                    continue;

                _rProps[p.Name] = p.PropertyType;
            }

            var validCtors = new Dictionary<ConstructorInfo, Int32>();
            var typeCtors = type.GetConstructors();

            foreach (var con in typeCtors)
            {
                var ctorParams = con.GetParameters();

                foreach (var p in ctorParams)
                {
                    if (!String.IsNullOrEmpty(p.Name))
                    {
                        // regular type (not runtime generated)
                        if (!_rProps.TryGetValue(p.Name, out var propType) ||
                            !p.ParameterType.IsAssignableFrom(propType))
                            goto ctorInvalid;
                        
                    }
                    else
                    {
                        isDynamicType = true;
                        goto dynamicType;

                    }

                    //if (String.IsNullOrEmpty(p.Name) ||
                    //    !_rProps.TryGetValue(p.Name, out var propType) ||
                    //    !p.ParameterType.IsAssignableFrom(propType))
                    //    goto ctorInvalid;
                }

                dynamicType:
                if (isDynamicType)
                {
                    _tProps ??= new Dictionary<Type, PropertyInfo>();

                    var allMyFields = type.GetFields(Const.AnyInstance);

                    foreach (var prop in type.GetProperties())
                    {
                        _tProps[prop.PropertyType] = prop;
                    }

                    foreach (var p in ctorParams)
                    {
                        if (!_tProps.ContainsKey(p.ParameterType))
                        {
                            if (allMyFields.Any(f => p.ParameterType.IsAssignableFrom(f.FieldType)))
                                continue;
                            
                            _tProps.Clear();
                            goto ctorInvalid;
                        }
                    }

                    _tProps.Clear();
                    _rProps.Clear();
                    return con;
                    //goto ctorInvalid;
                }

                // all ctor params are valid

                if (ctorParams.Length == _rProps.Count)
                {
                    //winner is you
                    _rProps.Clear();
                    return con;
                }

                validCtors.Add(con, ctorParams.Length);

                ctorInvalid: ;
            }

            _rProps.Clear();

            foreach (var item in validCtors.OrderByDescending(kvp => kvp.Value))
                return item.Key;

            //if (isDynamicType && typeCtors.Length > 0)
            //    return typeCtors[0];

            return default;
        }

        private static TypeConverter GetTypeConverter(Type type)
        {
            return _typeConverters.TryGetValue(type, out var found)
                ? found
                : _typeConverters.GetOrAdd(type, TypeDescriptor.GetConverter(type));
        }

        [MethodImpl(256)]
        private static Boolean IsLeafImpl(Type t,
                                          Boolean isStringCounts,
                                          ConcurrentDictionary<Type, Boolean> dic)
        {
            if (dic.TryGetValue(t, out var l))
                return l;

            return dic[t] = AmIALeaf(t, isStringCounts);
        }


        private static Boolean TryGetNullableTypeImpl(Type candidate,
                                                      out Type primitive)
        {
            if (!candidate.IsGenericType ||
                candidate.GetGenericTypeDefinition() != typeof(Nullable<>))
            {
                primitive = null!;
                return false;
            }

            primitive = candidate.GetGenericArguments()[0];
            return true;
        }

        [ThreadStatic]
        private static Dictionary<String, Type>? _rProps;

        [ThreadStatic]
        private static Dictionary<Type, PropertyInfo>? _tProps;


        private static readonly ConcurrentDictionary<Type, Boolean> CollectionTypes;

        private static readonly ConcurrentDictionary<Type, Boolean> _typesKnownSettableProperties;
        private static readonly ConcurrentDictionary<Type, Type> _cachedGermane;


        private static readonly ConcurrentDictionary<Type, ConstructorInfo?> CachedConstructors;

        private static readonly ConcurrentDictionary<Type, Boolean> LeavesNotString;
        private static readonly ConcurrentDictionary<Type, Boolean> LeavesYesString;

        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(Int32), typeof(Double), typeof(Decimal),
            typeof(Int64), typeof(Int16), typeof(SByte),
            typeof(Byte), typeof(UInt64), typeof(UInt16),
            typeof(UInt32), typeof(Single)
        };

        private static readonly ConcurrentDictionary<Type, Lazy<ICollection<PropertyInfo>>>
            CachedProperties;

        private static readonly ConcurrentDictionary<Type, TypeConverter> _typeConverters;

        protected ISerializerSettings _settings;
    }
}
