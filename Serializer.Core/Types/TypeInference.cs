using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Das.Serializer;
using Serializer;
using Serializer.Core;
using Das.CoreExtensions;

namespace Das.Types
{
    public class TypeInference : TypeCore, ITypeInferrer
    {
        public TypeInference(IDynamicTypes dynamicTypes, IAssemblyList assemblyList,
            ISerializerSettings settings) : base(settings)
        {
            _dynamicTypes = dynamicTypes;
            _assemblies = assemblyList;
            TypeNames = new ConcurrentDictionary<string, Type>();
            _cachedTypeNames = new ConcurrentDictionary<Type, string>();
            _cachedGermane = new ConcurrentDictionary<Type, Type>();
        }

        private readonly IDynamicTypes _dynamicTypes;
        private readonly ConcurrentDictionary<Type, Type> _cachedGermane;
        private readonly IAssemblyList _assemblies;
        private static readonly ConcurrentDictionary<Type, Object> CachedDefaults;
        private static readonly ConcurrentDictionary<Type, int> CachedSizes;
        

        static TypeInference()
        {
            CachedSizes = new ConcurrentDictionary<Type, int>();
            //bitconverter gives 1 byte.  SizeOf gives 4
            CachedSizes.TryAdd(typeof(Boolean), 1);
            CachedSizes.TryAdd(typeof(DateTime), 8);

            CachedDefaults = new ConcurrentDictionary<Type, object>();

            CachedDefaults.TryAdd(typeof(Byte), 0);
            CachedDefaults.TryAdd(typeof(Int16), 0);
            CachedDefaults.TryAdd(typeof(Int32), 0);
            CachedDefaults.TryAdd(typeof(Int64), 0);
            CachedDefaults.TryAdd(typeof(float), 0f);
            CachedDefaults.TryAdd(typeof(Double), 0.0);
            CachedDefaults.TryAdd(typeof(Decimal), 0M);
            CachedDefaults.TryAdd(typeof(DateTime), DateTime.MinValue);
            CachedDefaults.TryAdd(typeof(Boolean), false);
        }

        internal readonly ConcurrentDictionary<String, Type> TypeNames;
        private readonly ConcurrentDictionary<Type, String> _cachedTypeNames;

        private IEnumerable<Type> GetGenericTypes(String type)
        {
            var meat = ExtractGenericMeat(type, out var startIndex, out var endIndex);

            if (startIndex > 1)
            {
                //for the case of, zb, dictionary[string, list<int>] where we're at 
                //list<int> so it's a generic argument that has a generic argument(s)
                //but we have to avoid omitting the non-generic part
                yield return GetTypeFromClearName(type);
                yield break;
            }

            do
            {
                if (meat != null)
                {
                    yield return GetTypeFromClearName(meat);
                    var newStart = startIndex + endIndex + 1; //comma
                    if (newStart < type.Length)
                        type = type.Substring(newStart);
                    else break;
                }
                else if (type.Length > 0)
                    yield return GetTypeFromClearName(type);

                meat = ExtractGenericMeat(type, out startIndex, out endIndex);
            }
            while (startIndex >= 0);
        }

        private static String ExtractGenericMeat(String clearName, out Int32 startIndex,
            out Int32 endIndex)
        {
            startIndex = clearName.IndexOf('[');
            var genOpen = startIndex;

            endIndex = 0;

            if (startIndex < 0)
                return null;

            genOpen++;
            var depth = 1;
            for (var i = genOpen; i < clearName.Length; i++)
            {
                switch (clearName[i])
                {
                    case '[':
                        depth++;
                        break;
                    case ']':
                        depth--;
                        break;
                }

                if (depth != 0)
                    continue;

                endIndex = i;
                break;
            }
            startIndex++;
            return clearName.Substring(startIndex, endIndex - startIndex);

        }

        public string ToPropertyStyle(string name)
            => $"{Char.ToUpper(name[0])}{name.Substring(1)}";

        public string ToClearName(Type type, Boolean isOmitAssemblyName)
        {
            if (!isOmitAssemblyName && _cachedTypeNames.TryGetValue(type, out var name))
                return name;
            if (type.IsGenericType)
                name = GetClearGeneric(type, isOmitAssemblyName);
            else if (IsLeaf(type, true) && !type.IsEnum)
                name = type.Name;
            else if (!String.IsNullOrWhiteSpace(type.Namespace))
            {
                if (isOmitAssemblyName || type.Namespace?.StartsWith(Const.Tsystem) == true)
                    name = type.FullName;
                else
                    name = $"{type.Assembly.ManifestModule.Name},{type.FullName}";
            }
            else name = type.AssemblyQualifiedName;

            if (name == null)
                return name;

            if (!isOmitAssemblyName)
                _cachedTypeNames.TryAdd(type, name);
            TypeNames.TryAdd(name, type);
            return name;
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
                        var lastChanceDictionary = typeof(IDictionary<,>).
                            MakeGenericType(gargs);
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

        public Type GetGermaneType(object mustBeCollection)
        {
            if (mustBeCollection == null)
                throw new ArgumentNullException();
            return GetGermaneType(mustBeCollection.GetType());
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

        private String GetClearGeneric(Type type, Boolean isOmitAssemblyName)
        {
            StringBuilder sb;

            if (type.Namespace == null)
                return type.Name;

            if (isOmitAssemblyName || type.Namespace.StartsWith(Const.Tsystem))
                sb = new StringBuilder($"{type.Namespace}.{type.Name}");
            else
                sb = new StringBuilder($"{type.Assembly.ManifestModule.Name}, {type.Namespace}.{type.Name}");
            
            sb.Remove(sb.Length - 2, 2);
            foreach (var subType in type.GetGenericArguments())
            {
                sb.Append($"[{ToClearName(subType, isOmitAssemblyName)}]");
            }

            return sb.ToString();
        }

        public void ClearCachedNames()
        {
            TypeNames.Clear();
        }

        public int BytesNeeded(Type typ)
        {
            if (CachedSizes.TryGetValue(typ, out var length))
                return length;

            if (TryGetNullableType(typ, out var nType))
            {
                length = BytesNeeded(nType) + 1;
                CachedSizes[nType] = length;
                return length;
            }

            if (typ.IsValueType)
            {
                if (typ.IsEnum)
                    length += Marshal.SizeOf(Enum.GetUnderlyingType(typ));
                else
                    length += Marshal.SizeOf(typ);
                CachedSizes.TryAdd(typ, length);
            }
            else
            {
                //non value types are not a static length
                throw new InvalidOperationException();
            }

            return length;
        }
        

        public bool IsDefaultValue(object o)
        {
            switch (o)
            {
                case null:
                    return true;
                case String str:
                    return str == String.Empty;
                case DateTime dt:
                    return dt == DateTime.MinValue;
                case IConvertible conv:
                    return Convert.ToInt32(conv) == 0;
            }

           
            var t = o.GetType();

            if (!t.IsValueType || t == typeof(void))
                return true;

            if (t.IsEnum)
            {
                return Convert.ToInt32(o) == 0;
            }

            if (CachedDefaults.TryGetValue(t, out var def))
                return def.Equals(o);

            def = Activator.CreateInstance(t); //yuck
            //this has to be activator.  A value type is dynamically made 
            //made using only that!

            CachedDefaults.TryAdd(t, def);
            return def.Equals(o);
        }

        public Type GetTypeFromClearName(String clearName)
            => FromClearName(clearName, true, true);

        public Type FromClearName(String clearName, Boolean isRecurse,
            Boolean isTryGeneric)
        {
            if (String.IsNullOrWhiteSpace(clearName))
                return null;

            if (TypeNames.TryGetValue(clearName, out var type))
                return type;
            
            if (_dynamicTypes.TryGetDynamicType(clearName, out type))
                return type;

            if (isTryGeneric)
            {
                var genericArgs = DeriveGeneric(clearName, out var genericName);
                if (genericArgs.Count > 0)
                {
                    if (genericName == null)
                    {
                        //couldn't find the type of one of the generic parameters
                        return default;
                    }

                    var genericType = FromClearName(genericName, true, true);
                    if (genericType != null)
                    {
                        type = genericType.MakeGenericType(genericArgs.ToArray());
                        EnsureCached(clearName, type);
                        return type;
                    }
                }
            }

            var tokens = clearName.Split(',');
            if (tokens.Length == 1)
                type = GetNotAssemblyQualified(clearName, isRecurse);
            else
                type = FromAssemblyQualified(clearName, tokens);

            if (type == null)
            {
                if (isRecurse)
                    type = FromHailMary(clearName);
                else return default;
            }

            EnsureCached(clearName, type);

            return type;
        }

        private void EnsureCached(String clearName, Type type)
        {
            if (type != null)
                TypeNames.TryAdd(clearName, type);
        }

        private List<Type> DeriveGeneric(String clearName, out String generic)
        {
            var genericTypes = new List<Type>();
            var isSomeGeneric = false;

            var search = clearName;
            var genericMeat = ExtractGenericMeat(search, out var startIndex, out var endIndex);
            var firstStart = startIndex;
            while (startIndex >= 0)
            {
                //could be List[string], dictionary[int][string]				
                if (!String.IsNullOrWhiteSpace(genericMeat))
                {
                    isSomeGeneric = true;

                    var meaty = GetGenericTypes(genericMeat);
                    foreach (var meat in meaty)
                    {
                        if (meat == null)
                        {
                            generic = default;
                            return new List<Type> {null};
                        }
                        genericTypes.Add(meat);
                    }
                    
                    startIndex = endIndex + 1;
                    if (startIndex >= search.Length)
                        break;
                    search = search.Substring(startIndex);
                }

                genericMeat = ExtractGenericMeat(search, out startIndex, out endIndex);
                if (String.IsNullOrEmpty(genericMeat))
                    break;
            }


            //even if the generic type is not found we still have to remove the meat from the type
            if (isSomeGeneric)
            {
                var fsName = clearName.Substring(0, firstStart - 1);

                if (firstStart > 3 && clearName[firstStart - 3] == '`')
                    //from .FullName on a generic type
                    generic = fsName;
                else
                    generic = $"{fsName}`{genericTypes.Count}";
            }
            else generic = default;

            return genericTypes;
        }

        private Type GetNotAssemblyQualified(String clearName, Boolean isRecurse)
        {
            var tokens = clearName.Split('.');
            return tokens.Length == 1 
                ? FromSingleToken(clearName, isRecurse) 
                : FromNamespaceQualified(clearName, tokens, isRecurse);
        }

        /// <summary>
        /// Prepends type search namespaces to name and tries to find. From just a single token
        /// we can never find anything
        /// </summary>
        private Type FromSingleToken(String singleToken, Boolean isRecurse)
        {
            var arr = Settings.TypeSearchNameSpaces;

            for (var c = 0; c < arr.Length; c++)
            {
                var searchNs = arr[c] + "." + singleToken;
                var type = Type.GetType(searchNs);
                if (type != null)
                    return type;
            }

            if (!isRecurse)
                return default;

            /////////////////////
            
            for (var c = 0; c < arr.Length; c++)
            {
                var searchNs = arr[c] + "." + singleToken;
                var type = FromClearName(searchNs, false, false);

                if (type != null)
                    return type;
            }

            return default;
        }

        private Type FromNamespaceQualified(String nsName, String[] tokens,
            Boolean isRecurse)
        {
            var type = Type.GetType(nsName);
            if (type != null)
                return type;

            var assName = tokens.Take(0, tokens.Length - 1).ToString('.') + ".dll";
            var isFound = _assemblies.TryGetAssembly(assName, out var assFound);

            if (isFound)
            {
                type = assFound.GetType(nsName);
                if (type != null)
                    return type;
            }

            if (TryFind(nsName, _assemblies, out type))
                return type;

            if (isRecurse)
                return FromClearName(tokens.Last(), false, false);

            return default;
        }

        private Type FromAssemblyQualified(String clearName, String[] tokens)
        {
            Type type = null;

            if (_assemblies.TryGetAssembly(tokens[0], out var assembly))
            {
                type = assembly.GetType(tokens[1]) ?? 
                    Type.GetType($"{tokens[1]},{assembly.FullName}");
            }
            return type ?? Type.GetType(clearName) ?? GetTypeFromClearName(tokens[0]);
        }

        private Type FromHailMary(String clearName)
        {
            if (_dynamicTypes.TryGetFromAssemblyQualifiedName(clearName, out var type))
                return type;

            if (TryFind(clearName, _assemblies, out type))
                return type;

            if (TryFind(clearName, _assemblies.GetAll(), out type))
                return type;

            TypeNames.TryAdd(clearName, null);

            return default;
        }

        private static Boolean TryFind(String clearName, IEnumerable<Assembly> assemblies,
            out Type type)
        {
            foreach (var ass in assemblies)
            {
                type = ass.GetType(clearName);
                if (type != null)
                    return true;

                type = ass.GetTypes().FirstOrDefault(t => t.Name == clearName);
                if (type != null)
                    return true;
            }

            type = default;
            return false;
        }
    }
}



