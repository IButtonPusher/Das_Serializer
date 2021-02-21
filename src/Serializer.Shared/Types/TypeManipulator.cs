#if !GENERATECODE
using PropertySetter = System.Action<object, object>;
#else
#endif
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.Collections;
using Das.Serializer.Properties;

using TypeStructures = Das.Serializer.Collections.DoubleConcurrentDictionary<System.Type, Das.Serializer.ISerializationDepth, Das.Serializer.ITypeStructure>;

namespace Das.Serializer
{
    public partial class TypeManipulator : TypeCore,
                                           ITypeManipulator
    {
        static TypeManipulator()
        {
            _lockNewType = new Object();
            //_knownSensitive = new ConcurrentDictionary<Type, ITypeStructure>();
            //_knownInsensitive = new ConcurrentDictionary<Type, ITypeStructure>();

            _knownInsensitive2 = new TypeStructures();
            _knownSensitive2 = new TypeStructures();

            _cachedPropertyAccessors = new DoubleDictionary<Type, String, IPropertyAccessor>();

            _cachedAdders = new ConcurrentDictionary<Type, VoidMethod?>();
        }

        public TypeManipulator(ISerializerSettings settings)
            : base(settings)
        {
        }

        //public override Boolean HasSettableProperties(Type type)
        //{
        //    if (_knownSensitive.TryGetValue(type, out var result) &&
        //        result.Depth >= SerializationDepth.GetSetProperties)
        //        return result.PropertyCount > 0;

        //    return base.HasSettableProperties(type);
        //}

        public Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method)
        {
            List<Type> args = new(
                method.GetParameters().Select(p => p.ParameterType));
            Type delegateType;
            if (method.ReturnType == typeof(void))
                delegateType = Expression.GetActionType(args.ToArray());
            else
            {
                args.Add(method.ReturnType);
                delegateType = Expression.GetFuncType(args.ToArray());
            }

            return (Func<Object, Object[], Object>) Delegate.CreateDelegate(delegateType, null, method);
        }


        /// <summary>
        ///     Detects the Add, Enqueue, Push etc method for generic collections
        /// </summary>
        public MethodInfo? GetAddMethod<T>(IEnumerable<T> collection)
        {
            var cType = collection.GetType();

            if (typeof(ICollection<T>).IsAssignableFrom(cType))
                return typeof(ICollection<T>).GetMethod(nameof(ICollection<T>.Add), new[] {typeof(T)})!;

            if (typeof(IList).IsAssignableFrom(cType))
                return typeof(IList).GetMethod(nameof(IList.Add), new[] {typeof(T)})!;

            var gargs = cType.GetGenericArguments();

            var prmType = gargs.FirstOrDefault()
                          ?? Const.ObjectType;

            if (gargs.Length == 1)
            {
                var addOne = cType.GetMethod(nameof(IList.Add), new[] {prmType});
                if (addOne != null)
                    return addOne;


                var pcc = typeof(IProducerConsumerCollection<>).MakeGenericType(prmType);
                if (pcc.IsAssignableFrom(cType))
                    return pcc.GetMethod(nameof(IProducerConsumerCollection<Object>.TryAdd));
            }

            foreach (var meth in cType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (meth.ReturnType != typeof(void))
                    continue;
                var prms = meth.GetParameters();
                if (prms.Length != 1 || prms[0].ParameterType != prmType)
                    continue;

                return meth;
            }

            return null;
        }

        ///// <summary>
        ///// Tries to get the Add method for a
        ///// </summary>
        //private MethodInfo? GetAddMethodImpl(Type cType,
        //                                     Type genericArg)
        //{
        //    if (typeof(ICollection<T>).IsAssignableFrom(cType))
        //        return typeof(ICollection<T>).GetMethod(nameof(ICollection<T>.Add), new[] {typeof(T)})!;

        //    if (typeof(IList).IsAssignableFrom(cType))
        //        return typeof(IList).GetMethod(nameof(IList.Add), new[] {typeof(T)})!;

        //    var gargs = cType.GetGenericArguments();

        //    var prmType = gargs.FirstOrDefault()
        //                  ?? Const.ObjectType;

        //    if (gargs.Length == 1)
        //    {
        //        var addOne = cType.GetMethod(nameof(IList.Add), new[] {prmType});
        //        if (addOne != null)
        //            return addOne;


        //        var pcc = typeof(IProducerConsumerCollection<>).MakeGenericType(prmType);
        //        if (pcc.IsAssignableFrom(cType))
        //            return pcc.GetMethod(nameof(IProducerConsumerCollection<Object>.TryAdd));
        //    }

        //    foreach (var meth in cType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        //    {
        //        if (meth.ReturnType != typeof(void))
        //            continue;
        //        var prms = meth.GetParameters();
        //        if (prms.Length != 1 || prms[0].ParameterType != prmType)
        //            continue;

        //        return meth;
        //    }

        //    return null;
        //}


        public Boolean TryGetAddMethod(Type collectionType,
                                       out MethodInfo addMethod)
        {
            addMethod = GetAddMethodImpl(collectionType)!;
            return addMethod != null;
        }


        public MethodInfo GetAddMethod(Type cType)
        {
            var adder = GetAddMethodImpl(cType);
            return adder ?? throw new MissingMethodException(cType.FullName, "Add");
        }

        public VoidMethod? GetAdder(Type collectionType,
                                    Object exampleValue)
        {
            return _cachedAdders.GetOrAdd(collectionType, t => GetAdderImpl(t, exampleValue.GetType()));

            //var eType = exampleValue.GetType();

            //if (_cachedAdders.TryGetValue(collectionType, out var res))
            //    return res;

            //var addMethod = FindInvocableMethod(collectionType, _addMethodNames, new[] {eType});

            ////var addMethod = collectionType.GetMethod("Add", new[] {eType});

            ////if (addMethod == null)
            ////{
            ////    var interfaces = (from i in collectionType.GetInterfaces()
            ////        let args = i.GetGenericArguments()
            ////        where args.Length == 1
            ////              && args[0] == eType
            ////        select i).FirstOrDefault();

            ////    if (interfaces != null)
            ////        addMethod = interfaces.GetMethod("Add", new[] {eType});
            ////}

            //if (addMethod == null)
            //    return default;

            //return CreateMethodCaller(addMethod);
        }

        public Boolean TryGetAdder(IEnumerable collection,
                                   out VoidMethod adder)
        {
            adder = _cachedAdders.GetOrAdd(collection.GetType(), GetAdderImpl)!;
            return adder != null;
            //   return TODO_IMPLEMENT_ME;
        }

        /// <summary>
        ///     Gets a delegate to add an object to a non-generic collection
        /// </summary>
        public VoidMethod GetAdder(IEnumerable collection,
                                   Type? type = null)
        {
            if (type == null)
                type = collection.GetType();

            return _cachedAdders.GetOrAdd(type, GetAdderImpl) ??
                   throw new MissingMethodException(type.Name, nameof(IList.Add));
        }

        public Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
                                                       out Action<Object, Object?> setter)
        {
            var backingField = GetBackingField(propertyInfo);
            if (backingField == null)
            {
                setter = default!;
                return false;
            }

            setter = CreateFieldSetter(backingField);
            return true;
        }


        public IEnumerable<MethodInfo> GetInterfaceMethods(Type type)
        {
            foreach (var parentInterface in type.GetInterfaces())
            foreach (var pp in GetInterfaceMethods(parentInterface))
            {
                yield return pp;
            }

            foreach (var mi in type.GetMethods(InterfaceMethodBindings))
            {
                if (mi.IsPrivate)
                    continue;
                yield return mi;
            }
        }

        public MethodInfo? FindInvocableMethod(Type type,
                                               String methodName,
                                               Type[] paramTypes)
        {
            _singleStringFairy ??= new String[1];
            _singleStringFairy[0] = methodName;

            var res = FindInvocableMethodImpl(type, _singleStringFairy, paramTypes,
                _localTypeSearch ??= new HashSet<Type>());
            _localTypeSearch.Clear();
            return res;
        }


        //private static MethodInfo? FindInvocableMethodImpl(Type type,
        //                                                   String methodName,
        //                                                   Type[] paramTypes,
        //                                                   HashSet<Type> typeSearch)
        //{
        //    var res = type.GetMethod(methodName, paramTypes);
        //    if (res != null)
        //        return res;

        //    foreach (var implements in type.GetInterfaces())
        //    {
        //        if (!typeSearch.Add(implements))
        //            continue;

        //        res = FindInvocableMethodImpl(implements, methodName, paramTypes, typeSearch);
        //        if (res != null)
        //            return res;
        //    }

        //    return default;
        //}

        public MethodInfo? FindInvocableMethod(Type type,
                                               ICollection<String> possibleMethodNames,
                                               Type[] paramTypes)
        {
            var res = FindInvocableMethodImpl(type, possibleMethodNames, paramTypes,
                _localTypeSearch ??= new HashSet<Type>());
            _localTypeSearch.Clear();

            return res;
        }

        public IEnumerable<INamedField> GetPropertiesToSerialize(Type type,
                                                                 ISerializationDepth depth)
        {
            var str = GetTypeStructure(type, depth);
            foreach (var pi in str.GetMembersToSerialize(depth))
            {
                yield return pi;
            }
        }

        public Type? GetPropertyType(Type classType,
                                     String propName)
        {
            var ts = GetTypeStructure(classType, DepthConstants.AllProperties);
            return ts.MemberTypes.TryGetValue(propName, out var res) ? res.Type : default;
        }

        IEnumerable<FieldInfo> ITypeManipulator.GetRecursivePrivateFields(Type type)
        {
            return GetRecursivePrivateFields(type);
        }

        public ITypeStructure GetStructure<T>(ISerializationDepth depth)
        {
            return GetTypeStructure(typeof(T), depth);
        }

        public ITypeStructure GetTypeStructure(Type type,
                                               ISerializationDepth depth)
        {
            return Settings.IsPropertyNamesCaseSensitive
                ? _knownSensitive2.GetOrAdd(type, depth, BuildCaseSensitiveTypeStructure)
                : _knownInsensitive2.GetOrAdd(type, depth, BuildCaseInsensitiveTypeStructure);

            //if (Settings.IsPropertyNamesCaseSensitive)
            //    return ValidateCollection(type, depth, true);

            //return ValidateCollection(type, depth, false);
        }

        private ITypeStructure BuildCaseSensitiveTypeStructure(Type type,
                                                               ISerializationDepth depth)
        {
            return new TypeStructure(type, true, depth, this);
        }

        private ITypeStructure BuildCaseInsensitiveTypeStructure(Type type,
                                                               ISerializationDepth depth)
        {
            return new TypeStructure(type, false, depth, this);
        }

        public Type InstanceMemberType(MemberInfo info)
        {
            switch (info)
            {
                case PropertyInfo prop:
                    return prop.PropertyType;
                case FieldInfo field:
                    return field.FieldType;
                default:
                    throw new InvalidOperationException();
            }
        }

        //public abstract bool TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
        //                                                     out Action<object, object?> setter);

        //public abstract bool TryGetAddMethod(Type collectionType,
        //                                     out MethodInfo addMethod);

        //public abstract VoidMethod CreateMethodCaller(MethodInfo method);


        public IPropertyAccessor GetPropertyAccessor(Type declaringType,
                                                     String propertyName)
        {
            lock (_lockNewType)
            {
                if (_cachedPropertyAccessors.TryGetValue(declaringType, propertyName, out var accessor))
                    return accessor;

                var getter = CreatePropertyGetter(declaringType, propertyName, out var propInfo);
                var setter = CreateSetMethod(declaringType, propertyName);
                accessor = new SimplePropertyAccessor(declaringType, propertyName,
                    getter, setter, propInfo);

                _cachedPropertyAccessors.Add(declaringType, propertyName, accessor);

                return accessor;
            }
        }

        public IPropertyAccessor<T> GetPropertyAccessor<T>(String propertyName)
        {
            var declaringType = typeof(T);

            lock (_lockNewType)
            {
                if (_cachedPropertyAccessors.TryGetValue(declaringType, propertyName, out var accessor) &&
                    accessor is IPropertyAccessor<T> accessor2)
                    return accessor2;

                var getter = CreatePropertyGetter(declaringType, propertyName, out var propInfo);
                var setter = CreateSetMethod<T>(propertyName);
                accessor2 = new PropertyAccessor<T>(propertyName,
                    getter, setter, propInfo);

                _cachedPropertyAccessors.Add(declaringType, propertyName, accessor);

                return accessor2;
            }
        }

        public static IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type)
        {
            while (true)
            {
                foreach (var field in type.GetFields(Const.NonPublic))
                {
                    yield return field;
                }

                var parent = type.BaseType;
                if (parent == null) yield break;

                type = parent;
            }
        }

        protected static MemberInfo[] GetMembersOrDie(Type declaringType,
                                                      String propertyName)
        {
            var membersOnly = declaringType.GetMember(propertyName);
            if (membersOnly.Length == 0)
                throw new MissingMemberException(declaringType.FullName, propertyName);

            return membersOnly;
        }

        protected static PropertyInfo GetPropertyOrDie(Type declaringType,
                                                       String propertyName)
        {
            return declaringType.GetProperty(propertyName) ??
                   throw new MissingMemberException(declaringType.FullName, propertyName);
        }


        protected static Boolean TryGetMembers(Type declaringType,
                                               String propertyName,
                                               out MemberInfo[] membersOnly)
        {
            membersOnly = declaringType.GetMember(propertyName);
            return membersOnly.Length > 0;
        }

        private static VoidMethod CreateAddDelegate( //ICollection collection,
            Type type)
        {
            //var colType = collection.GetType();

            //if (_cachedAdders.TryGetValue(colType, out var res))
            //    return res;

            VoidMethod? res;

            //super sophisticated
            var method = type.GetMethod(nameof(IList.Add));
            if (method != null)
            {
                #if GENERATECODE

                var dynam = CreateMethodCaller(method, true);
                res = (VoidMethod) dynam.CreateDelegate(typeof(VoidMethod));

                #else
                res = CreateMethodCaller(method);

                #endif
            }
            else res = default;

            //_cachedAdders.TryAdd(colType, res!);

            return res!;
        }

        #if NET40 || NET45

        ///// <summary>
        /////     Gets a delegate to add an object to a generic collection
        ///// </summary>
        //public VoidMethod CreateAddDelegate<T>(IEnumerable<T> collection)
        //{
        //    var colType = collection.GetType();

        //    if (_cachedAdders.TryGetValue(colType, out var res))
        //        return res;

        //    var method = GetAddMethod(collection);

        //    if (method != null)
        //    {
        //        res = CreateAddDelegateImpl(method);
        //        //#if GENERATECODE

        //        //var dynam = CreateMethodCaller(method, true);
        //        //res = (VoidMethod) dynam.CreateDelegate(typeof(VoidMethod));

        //        //#else
        //        //res = CreateMethodCaller(method);

        //        //#endif
        //    }

        //    _cachedAdders.TryAdd(colType, res!);

        //    return res!;
        //}

        private static VoidMethod CreateAddDelegateImpl(MethodInfo method)
        {
            #if GENERATECODE

            var dynam = CreateMethodCaller(method, true);
            return (VoidMethod) dynam.CreateDelegate(typeof(VoidMethod));

            #else
                return CreateMethodCaller(method);

            #endif
        }

        #endif

        private static MethodInfo? FindInvocableMethodImpl(Type type,
                                                           ICollection<String> possibleMethodNames,
                                                           Type[] paramTypes,
                                                           HashSet<Type> typeSearch)
        {
            //MethodInfo? res = null;

            var methods = type.GetMethods();

            foreach (var m in methods)
            {
                if (!possibleMethodNames.Contains(m.Name))
                    continue;

                var mParams = m.GetParameters();
                if (mParams.Length != paramTypes.Length)
                    continue;

                var c = 0;

                for (; c < mParams.Length; c++)
                    if (mParams[c].ParameterType != paramTypes[c])
                        break;

                if (c < mParams.Length - 1)
                    continue;

                return m;
            }


            foreach (var implements in type.GetInterfaces())
            {
                if (!typeSearch.Add(implements))
                    continue;

                var res = FindInvocableMethodImpl(implements, possibleMethodNames, paramTypes, typeSearch);
                if (res != null)
                    return res;
            }

            return default;
        }

        private VoidMethod? GetAdderImpl(Type collectionType,
                                         Type germaneType)
        {
            var addMethod = FindInvocableMethod(collectionType, _addMethodNames, new[] {germaneType});
            if (addMethod == null)
                return default;

            return CreateMethodCaller(addMethod);
        }

        private VoidMethod? GetAdderImpl(Type type)
        {
            #if NET40 || NET45
            if (type.IsGenericType)
            {
                var gargs = type.GetGenericArguments();
                MethodInfo? addMethod;

                switch (gargs.Length)
                {
                    case 1:
                        addMethod = FindInvocableMethod(type, _addMethodNames, new[] {gargs[0]});
                        break;

                    case 2:
                        var kvp = typeof(KeyValuePair<,>).MakeGenericType(gargs[0], gargs[1]);
                        addMethod = FindInvocableMethod(type, nameof(IList.Add), new[] {kvp});
                        break;

                    default:
                        throw new NotImplementedException();
                }

                if (addMethod != null)
                    return CreateAddDelegateImpl(addMethod);

                return default;
                //throw new MissingMethodException(nameof(IList.Add));
            }
            #endif

            //if (collection is ICollection icol)
            //{
            //    return CreateAddDelegate(//icol, 
            //        type);
            //}

            //var boxList = new List<Object>(collection.OfType<Object>());
            return CreateAddDelegate( //boxList, 
                type);
        }

        private MethodInfo? GetAddMethodImpl(Type cType)
        {
            var germane = GetGermaneType(cType);

            if (cType.TryGetMethod(nameof(ICollection<Object>.Add), out var adder, germane))
                return adder;

            if (typeof(List<>).IsAssignableFrom(cType) ||
                typeof(Dictionary<,>).IsAssignableFrom(cType))
                return cType.GetMethodOrDie(nameof(List<Object>.Add));

            if (typeof(Stack<>).IsAssignableFrom(cType))
                return cType.GetMethodOrDie(nameof(Stack<Object>.Push));

            if (typeof(Queue<>).IsAssignableFrom(cType))
                return cType.GetMethodOrDie(nameof(Queue<Object>.Enqueue));

            if (typeof(IDictionary).IsAssignableFrom(cType))
            {
                var gDic = typeof(IDictionary<,>).MakeGenericType(cType.GetGenericArguments());
                return gDic.GetMethodOrDie(nameof(IDictionary<Object, Object>.Add));
            }

            return default;
        }

        private static FieldInfo? GetBackingField(PropertyInfo pi)
        {
            if (pi == null)
                return null;

            var compGen = typeof(CompilerGeneratedAttribute);

            var decType = pi.DeclaringType;

            if (decType == null || !pi.CanRead ||
                pi.GetGetMethod(true)?.IsDefined(compGen, true) != true)
                return null;
            var backingField = decType.GetField($"<{pi.Name}>k__BackingField",
                Const.NonPublic);
            if (backingField == null)
                return null;
            if (backingField.IsDefined(compGen, true))
                return backingField;

            var flds = GetRecursivePrivateFields(decType).ToArray();

            if (flds.Length == 0)
                return null;
            var name = $"<{pi.Name}>";

            backingField = flds.FirstOrDefault(f => f.Name.Contains(name))
                           ?? flds.FirstOrDefault(f => f.Name.IndexOf(name,
                               StringComparison.OrdinalIgnoreCase) >= 0);

            if (backingField == null || backingField.FieldType != pi.PropertyType)
                return null;

            return backingField;
        }

        private static IEnumerable<PropertyInfo> GetPropertyChain(Type declaringType,
                                                                  String propName)
        {
            if (!propName.Contains("."))
            {
                yield return GetPropertyOrDie(declaringType, propName);
                yield break;
            }

            var subPropTokens = propName.Split('.');
            var propInfo = GetPropertyOrDie(declaringType, subPropTokens[0]);
            yield return propInfo;

            for (var c = 1; c < subPropTokens.Length; c++)
            {
                propInfo = GetPropertyOrDie(propInfo.PropertyType, subPropTokens[c]);
                yield return propInfo;
            }
        }

        //private static Boolean IsAlreadyExists(Type type,
        //                                       Boolean doCache,
        //                                       ISerializationDepth depth,
        //                                       ConcurrentDictionary<Type, ITypeStructure> collection,
        //                                       out ITypeStructure res)
        //{
        //    res = default!;

        //    if (!doCache || !collection.TryGetValue(type, out res))
        //        return false;

        //    if (res.Depth < depth.SerializationDepth) 
        //        return false;


        //    if (doCache && collection.TryGetValue(type, out res) &&
        //        res.Depth >= depth.SerializationDepth)
        //        return true;

        //    res = default!;
        //    return false;
        //}

        //private ITypeStructure ValidateCollection(Type type,
        //                                          ISerializationDepth depth,
        //                                          Boolean caseSensitive)
        //{
        //    var collection = caseSensitive ? _knownSensitive : _knownInsensitive;

        //    var doCache = Settings.CacheTypeConstructors;


        //    if (IsAlreadyExists(type, doCache, depth, collection, out var result))
        //        return result;

        //    lock (_lockNewType)
        //    {
        //        if (IsAlreadyExists(type, doCache, depth, collection, out result))
        //            return result;

        //        result = new TypeStructure(type, caseSensitive, depth, this); //, pool);
        //        if (!doCache)
        //            return result;

        //        return collection.AddOrUpdate(type, result, (_,
        //                                                     v) => v.Depth > result.Depth ? v : result);
        //    }
        //}

        private const BindingFlags InterfaceMethodBindings = BindingFlags.Instance |
                                                             BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Object _lockNewType;
        //private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownSensitive;
        //private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownInsensitive;


        private static readonly TypeStructures _knownInsensitive2;
        private static readonly TypeStructures _knownSensitive2;

        private static readonly DoubleDictionary<Type, String, IPropertyAccessor> _cachedPropertyAccessors;

        private static readonly String[] _addMethodNames =
        {
            nameof(IList.Add),
            nameof(BlockingCollection<Object>.TryAdd),
            nameof(Queue.Enqueue),
            nameof(Stack.Push)
        };

        [ThreadStatic]
        private static PropertyInfo[]? _singlePropFairy;

        [ThreadStatic]
        private static String[]? _singleStringFairy;

        [ThreadStatic]
        private static Type[]? _paramTypeFairy;

        [ThreadStatic]
        private static HashSet<Type>? _localTypeSearch;

        private static readonly ConcurrentDictionary<Type, VoidMethod?> _cachedAdders;
    }
}
