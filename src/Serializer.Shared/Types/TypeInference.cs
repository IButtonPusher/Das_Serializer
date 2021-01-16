using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;

namespace Das.Types
{
    public class TypeInference : TypeCore, 
                                 ITypeInferrer
    {
        static TypeInference()
        {
            CachedSizes = new Dictionary<Type, Int32>();

            //bitconverter gives 1 byte.  SizeOf gives 4
            CachedSizes[typeof(Boolean)] = 1;
            CachedSizes[typeof(DateTime)] = 8;
            CachedSizes[typeof(Single)] = 4;

            CachedDefaults = new ConcurrentDictionary<Type, Object>();

            CachedDefaults.TryAdd(typeof(Byte), 0);
            CachedDefaults.TryAdd(typeof(Int16), 0);
            CachedDefaults.TryAdd(Const.IntType, 0);
            CachedDefaults.TryAdd(typeof(Int64), 0);
            CachedDefaults.TryAdd(typeof(Single), 0f);
            CachedDefaults.TryAdd(typeof(Double), 0.0);
            CachedDefaults.TryAdd(typeof(Decimal), 0M);
            CachedDefaults.TryAdd(typeof(DateTime), DateTime.MinValue);
            CachedDefaults.TryAdd(typeof(Boolean), false);


            TypeNames = new ConcurrentDictionary<String, Type?>();
            TypeNames["object"] = Const.ObjectType;
            TypeNames["string"] = typeof(String);
            TypeNames["bool"] = typeof(Boolean);
            TypeNames["byte"] = typeof(Byte);
            TypeNames["char"] = typeof(Char);
            TypeNames["decimal"] = typeof(Decimal);
            TypeNames["double"] = typeof(Double);
            TypeNames["short"] = typeof(Int16);
            TypeNames["int"] = Const.IntType;
            TypeNames["long"] = typeof(Int64);
            TypeNames["sbyte"] = typeof(SByte);
            TypeNames["float"] = typeof(Single);
            TypeNames["ushort"] = typeof(UInt16);
            TypeNames["uint"] = typeof(UInt32);
            TypeNames["ulong"] = typeof(UInt64);

            foreach (var typ in TypeNames.Values)
            {
                if (typ?.IsValueType != true)
                    continue;


                if (typ.IsEnum)
                {
                    var length = Marshal.SizeOf(Enum.GetUnderlyingType(typ));
                    CachedSizes[typ] = length;
                }
                else
                {
                    var length = Marshal.SizeOf(typ);
                    CachedSizes[typ] = length;
                }
            }

            CachedSizes[typeof(Boolean)] = 1;


            _cachedTypeNames = new ConcurrentDictionary<Type, String>();
        }

        public TypeInference(IDynamicTypes dynamicTypes, 
                             IAssemblyList assemblyList,
                             ISerializerSettings settings) : base(settings)
        {
            _dynamicTypes = dynamicTypes;
            _assemblies = assemblyList;
        }

        /// <summary>
        /// Returns the name in PascalCase
        /// </summary>
        public String ToPropertyStyle(String name)
        {
            switch (name.Length)
            {
                case 0:
                    return name;
                
                case 1:
                    return name.ToUpper();
                
                default:
                    return $"{Char.ToUpper(name[0])}{name.Substring(1)}";
            }
        }


        public String ToClearName(Type type, 
                                  Boolean isOmitAssemblyName)
        {
            if (!isOmitAssemblyName && _cachedTypeNames.TryGetValue(type, out var name))
                return name;
            if (type.IsGenericType)
            {
                name = GetClearGeneric(type, isOmitAssemblyName);
            }
            else if (IsLeaf(type, true) && !type.IsEnum)
            {
                name = type.Name;
            }
            else if (!String.IsNullOrWhiteSpace(type.Namespace))
            {
                if (isOmitAssemblyName || type.Namespace?.StartsWith(Const.Tsystem) == true)
                    name = type.FullName;
                else
                    name = $"{type.Assembly.ManifestModule.Name},{type.FullName}";
            }
            else
            {
                name = type.AssemblyQualifiedName;
            }

            if (name == null)
                return type.Name;
            //return name;

            if (!isOmitAssemblyName)
                _cachedTypeNames.TryAdd(type, name);
            TypeNames.TryAdd(name, type);
            return name;
        }

        public void ClearCachedNames()
        {
            TypeNames.Clear();
        }

        public Int32 BytesNeeded(Type typ)
        {
            if (CachedSizes.TryGetValue(typ, out var length))
                return length;

            if (TryGetNullableType(typ, out var nType) &&
                nType != null)
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
            }
            else
            {
                //non value types are not a static length
                throw new InvalidOperationException();
            }

            return length;
        }


       

        public Boolean IsDefaultValue<T>(T value)
        {
            if (ReferenceEquals(null, value))
                return true;

            var typo = typeof(T);

            if (typo != typeof(Object))
                return EqualityComparer<T>.Default.Equals(value);

            typo = value.GetType();

            if (!typo.IsValueType)
                return false;
            //return ReferenceEquals(null, value);

            if (typo.IsEnum)
                return Convert.ToInt32(value) == 0;

            if (CachedDefaults.TryGetValue(typo, out var def))
                return def.Equals(value);

            def = Activator.CreateInstance(typo); //yuck
            //this has to be activator.  A value type is dynamically made 
            //made using only that!

            CachedDefaults.TryAdd(typo, def);
            return def.Equals(value);
        }

        public Type? GetTypeFromClearName(String clearName, 
                                          IDictionary<string, string> nameSpaceAssemblySearch, 
                                          Boolean isTryGeneric = false)
        {
            return FromClearName(clearName, true, isTryGeneric, false, nameSpaceAssemblySearch);
        }
        
       
        public Type? GetTypeFromClearName(String clearName, 
                                          Boolean isTryGeneric = false)
        {
            return FromClearName(clearName, true, isTryGeneric, false, null);
        }

        private Type?[] DeriveGeneric(String clearName, 
                                      IDictionary<string, string>? nameSpaceAssemblySearch, 
                                      out String? generic)
        {
            var isSomeGeneric = false;

            var search = clearName;
            var genericMeat = ExtractGenericMeat(search, out var startIndex, out var endIndex);
            var firstStart = startIndex;

            if (startIndex < 0)
            {
                generic = default;
                return Type.EmptyTypes;
            }

            var genericTypes = new List<Type>();

            while (startIndex >= 0)
            {
                //could be List[string], dictionary[int][string]				
                if (!String.IsNullOrWhiteSpace(genericMeat))
                {
                    isSomeGeneric = true;

                    var meaty = GetGenericTypes(genericMeat!, nameSpaceAssemblySearch);
                    foreach (var meat in meaty)
                    {
                        if (meat == null)
                        {
                            generic = default;
                            return new Type?[] {null};
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
            else
            {
                generic = default;
            }

            return genericTypes.ToArray();
        }

        private static void EnsureCached(String clearName, Type? type)
        {
            if (type != null)
                TypeNames.TryAdd(clearName, type);
        }

        private static String? ExtractGenericMeat(String clearName, 
                                                  out Int32 startIndex,
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

        private Type? FromAssemblyQualified(String clearName, 
                                            String[] tokens)
        {
            Type? type = null;

            if (_assemblies.TryGetAssembly(tokens[0], out var assembly))
                type = assembly.GetType(tokens[1]) ??
                       Type.GetType($"{tokens[1]},{assembly.FullName}");

            return type ?? Type.GetType(clearName) ?? GetTypeFromClearName(tokens[0]);
        }

        private Type? FromClearName(String clearName,
                                    Boolean isRecurse,
                                    Boolean isTryGeneric,
                                    Boolean isSearchLoadedAssemblies,
                                    IDictionary<String, String>? nameSpaceAssemblySearch)
        {
            if (String.IsNullOrWhiteSpace(clearName))
                return null;

            if (TypeNames.TryGetValue(clearName, out var type))
                return type;

            if (_dynamicTypes.TryGetDynamicType(clearName, out type))
                return type;

            if (isTryGeneric)
            {
                var genericArgs = DeriveGeneric(clearName,
                    nameSpaceAssemblySearch,
                    out var genericName);
                if (genericArgs.Length > 0)
                {
                    if (genericName == null)
                        //couldn't find the type of one of the generic parameters
                        return default;

                    var genericType = FromClearName(genericName, true, true, 
                        isSearchLoadedAssemblies, nameSpaceAssemblySearch);
                    if (genericType != null)
                    {
                        type = genericType.MakeGenericType(genericArgs);
                        EnsureCached(clearName, type);
                        return type;
                    }
                }
            }

            var tokens = clearName.Split(',');
            if (tokens.Length == 1)
                type = GetNotAssemblyQualified(clearName, isRecurse, 
                    isSearchLoadedAssemblies, nameSpaceAssemblySearch);
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

        private Type? FromHailMary(String clearName)
        {
            Type? type;

            if (_dynamicTypes.TryGetFromAssemblyQualifiedName(clearName, out type))
                return type;

            if (TryFind(clearName, _assemblies, out type))
                return type;

            if (TryFind(clearName, _assemblies.GetAll(), out type))
                return type;

            TypeNames.TryAdd(clearName, null);

            return default;
        }

        private Type? FromNamespaceQualified(String nsName, 
                                             String[] tokens,
                                             Boolean isRecurse,
                                             Boolean isSearchLoadedAssemblies,
                                             IDictionary<String, String>? nameSpaceAssemblySearch)
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

            if (isSearchLoadedAssemblies && TryFind(nsName, _assemblies, out type))
                return type;

            if (isRecurse)
                return FromClearName(tokens.Last(), false, false, 
                    isSearchLoadedAssemblies, nameSpaceAssemblySearch);

            return default;
        }

        /// <summary>
        ///     Prepends type search namespaces to name and tries to find. From just a single token
        ///     we can never find anything
        /// </summary>
        private Type? FromSingleToken(String singleToken, 
                                      Boolean isRecurse,
                                      IDictionary<String, String>? nameSpaceAssemblySearch)
        {
            if (nameSpaceAssemblySearch is { } search)
            {
                foreach (var kvp in search)
                {
                    if (!_assemblies.TryGetAssembly(kvp.Value, out var asm)) 
                        continue;
                    
                    var searchNs = kvp.Key  + "." + singleToken;
                    var type = asm.GetType(searchNs);
                    if (type != null)
                        return type;

                    //var omgbbq = asm.GetTypes().OrderBy(t => t.Name).ToArray();
                    //var plz = omgbbq.FirstOrDefault(t => t.Name == singleToken);

                }
            }
            
            var arr = Settings.TypeSearchNameSpaces;

            for (var c = 0; c < arr.Length; c++)
            {
                var searchNs = arr[c] + "." + singleToken;
                var type = Type.GetType(searchNs, false, true);
                if (type != null)
                    return type;
            }

            if (!isRecurse)
                return default;

            for (var c = 0; c < arr.Length; c++)
            {
                var searchNs = arr[c] + "." + singleToken;
                var type = FromClearName(searchNs, false, false, false, nameSpaceAssemblySearch);

                if (type != null)
                    return type;
            }
            
            
            for (var c = 0; c < arr.Length; c++)
            {
                var searchNs = arr[c] + "." + singleToken;
                var type = FromClearName(searchNs, false, false, true, nameSpaceAssemblySearch);

                if (type != null)
                    return type;
            }

            return default;
        }


        private String GetClearGeneric(Type type, 
                                       Boolean isOmitAssemblyName)
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
                sb.Append($"[{ToClearName(subType, isOmitAssemblyName)}]");

            return sb.ToString();
        }

        private IEnumerable<Type?> GetGenericTypes(String type,
                                                   IDictionary<String, String>? nameSpaceAssemblySearch)
        {
            var meat = ExtractGenericMeat(type, out var startIndex, out var endIndex);

            if (startIndex > 1)
            {
                //for the case of, zb, dictionary[string, list<int>] where we're at 
                //list<int> so it's a generic argument that has a generic argument(s)
                //but we have to avoid omitting the non-generic part

                if (nameSpaceAssemblySearch is {} search)
                    yield return GetTypeFromClearName(type, search, true);
                else
                    yield return GetTypeFromClearName(type, true);
                yield break;
            }

            do
            {
                if (meat != null)
                {
                    if (nameSpaceAssemblySearch is {} search)
                        yield return GetTypeFromClearName(meat, search);
                    else
                        yield return GetTypeFromClearName(meat);
                    var newStart = startIndex + endIndex + 1; //comma
                    if (newStart < type.Length)
                        type = type.Substring(newStart);
                    else break;
                }
                else if (type.Length > 0)
                {
                    if (nameSpaceAssemblySearch is {} search)
                        yield return GetTypeFromClearName(type, search);
                    else
                        yield return GetTypeFromClearName(type);
                }

                meat = ExtractGenericMeat(type, out startIndex, out endIndex);
            } while (startIndex >= 0);
        }

        private Type? GetNotAssemblyQualified(String clearName, 
                                              Boolean isRecurse,
                                              Boolean isSearchLoadedAssemblies,
                                              IDictionary<String, String>? nameSpaceAssemblySearch)
        {
            var tokens = clearName.Split('.');
            return tokens.Length == 1
                ? FromSingleToken(clearName, isRecurse, nameSpaceAssemblySearch)
                : FromNamespaceQualified(clearName, tokens, 
                    isRecurse, isSearchLoadedAssemblies, nameSpaceAssemblySearch);
        }

        private static Boolean TryFind(String clearName, 
                                       IEnumerable<Assembly> assemblies,
                                       out Type? type)
        {
            foreach (var asm in assemblies)
                try
                {
                    type = asm.GetType(clearName);
                    if (type != null)
                        return true;

                    type = asm.GetTypes().FirstOrDefault(t => t.Name == clearName);
                    if (type != null)
                        return true;
                }
                catch (ReflectionTypeLoadException)
                {
                }

            type = default;
            return false;
        }

        private static readonly ConcurrentDictionary<Type, Object> CachedDefaults;
        private static readonly Dictionary<Type, Int32> CachedSizes;

        private static readonly ConcurrentDictionary<String, Type?> TypeNames;
        private static readonly ConcurrentDictionary<Type, String> _cachedTypeNames;
        
        private readonly IAssemblyList _assemblies;


        private readonly IDynamicTypes _dynamicTypes;
    }
}