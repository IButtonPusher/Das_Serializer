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
using Das.Serializer.Types;
using Reflection.Common;
//using TypeStructures = Das.Serializer.Collections.DoubleConcurrentDictionary<System.Type, Das.Serializer.ISerializationDepth, Das.Serializer.ITypeStructure>;

namespace Das.Serializer
{
    public partial class TypeManipulator : TypeCore,
                                           ITypeManipulator
    {
        static TypeManipulator()
        {
            _lockNewType = new Object();
            _knownSensitive = new ConcurrentDictionary<Type, ITypeStructure>();

            _cachedPropertyAccessors = new DoubleDictionary<Type, String, IPropertyAccessor>();
            _cachedTypeAccessors = new Dictionary<Type, TypePropertiesAccessor>();

            _cachedAdders = new ConcurrentDictionary<Type, VoidMethod?>();

            _knownGeneric = new ConcurrentDictionary<Type, ITypeStructure>();
        }

        public TypeManipulator(ISerializerSettings settings)
            : base(settings)
        {
        }


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

        PropertySetter? ITypeManipulator.CreateSetMethod(MemberInfo memberInfo)
        {
            return CreateSetMethod(memberInfo);
        }

        Func<object, object> ITypeManipulator.CreatePropertyGetter(Type targetType,
                                                                   PropertyInfo propertyInfo)
        {
            return CreatePropertyGetter(targetType, propertyInfo);
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

        VoidMethod ITypeManipulator.CreateMethodCaller(MethodInfo method)
        {
            return CreateMethodCaller(method);
        }

       
      

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
                                                                 SerializationDepth depth)
        {
            var str = GetTypeStructure(type);//, depth);
            foreach (var pi in str.GetMembersToSerialize(depth))
            {
                yield return pi;
            }
        }

        public Type? GetPropertyType(Type classType,
                                     String propName)
        {
            var ts = GetTypeStructure(classType);//, DepthConstants.AllProperties);
            if (ts.TryGetPropertyAccessor(propName, PropertyNameFormat.Default, out var accessor))
                return accessor.PropertyType;

            return default;
            //return ts.MemberTypes.TryGetValue(propName, out var res) ? res.Type : default;
        }

        IEnumerable<FieldInfo> ITypeManipulator.GetRecursivePrivateFields(Type type)
        {
            return GetRecursivePrivateFields(type);
        }

        public ITypeStructure GetTypeStructure(Type type)
        {
            return _knownSensitive.GetOrAdd(type, BuildTypeStructureImpl);
        }

        public ITypeStructure<T> GetTypeStructure<T>()
        {
            var res = _knownGeneric.GetOrAdd(typeof(T), _ => BuildTypeStructureImpl<T>());
            return (ITypeStructure<T>)res;
        }

        private ITypeStructure<T> BuildTypeStructureImpl<T>()
        {
            var accessors = BuildPropertyAccessors<T>();
            return new TypeStructure<T>(typeof(T), this, accessors);
        }

        private ITypeStructure BuildTypeStructureImpl(Type type)
        {
            var accessors = BuildPropertyAccessors(type);
            return new TypeStructure(type, this, accessors);
        }

        private IEnumerable<IPropertyAccessor<T>> BuildPropertyAccessors<T>()
        {
            foreach (var pi in GetValidProperties(typeof(T)))
            {
                var propAccessor = GetPropertyAccessor<T>(pi.Name);
                yield return propAccessor;
            }
        }

        private IEnumerable<IPropertyAccessor> BuildPropertyAccessors(Type type)
        {
           var accessors = GetTypePropertyAccessor(type);
           return accessors;
        }

        public static IEnumerable<PropertyInfo> GetValidProperties(Type type)
        {
            
            if (type.IsDefined(typeof(SerializeAsTypeAttribute), false))
            {
                var serAs = type.GetCustomAttributes(typeof(SerializeAsTypeAttribute), 
                   true).First() as SerializeAsTypeAttribute;

                if (serAs?.TargetType != null)
                    type = serAs.TargetType;
            }

            foreach (var pi in GetPublicProperties(type))
            {
                if (pi.GetIndexParameters().Length > 0)
                    // index properties don't fit into the current Func<Object, Object> paradigm...
                    continue;

                yield return pi;
            }
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




        // ReSharper disable once MemberCanBeMadeStatic.Global
        public TypePropertiesAccessor GetTypePropertyAccessor(Type declaringType)
        {
           lock (_lockNewType)
           {
              if (!_cachedTypeAccessors.TryGetValue(declaringType, out var typeAccessor))
              {
                 typeAccessor = new TypePropertiesAccessor(declaringType,
                    GetValidProperties(declaringType));
                 _cachedTypeAccessors[declaringType] = typeAccessor;
              }

              return typeAccessor;
           }
        }

        public IPropertyAccessor GetPropertyAccessor(Type declaringType,
                                                     String propertyName)
        {

           if (propertyName.IndexOf('.') == -1)
           {
              var typeAccessor = GetTypePropertyAccessor(declaringType);
              return typeAccessor[propertyName];
           }

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


        public IPropertyAccessor<TObject, TProperty> GetPropertyAccessor<TObject, TProperty>(String propName)
        {
            return PropertyDictionary<TObject, TProperty>.Properties.GetOrAdd(propName, 
                BuildPropertyAccessor<TObject, TProperty>);
        }

        private IPropertyAccessor<TObject, TProperty> BuildPropertyAccessor<TObject, TProperty>(String propName)
        {
            var getter = CreatePropertyGetter<TObject, TProperty>(propName, out var propInfo);
            return new PropertyAccessor<TObject, TProperty>(propInfo, getter, null, propName);
        }

        public static IPropertyAccessor<T> BuildPropertyAccessor<T>(String propertyName)
        {
           var getter = CreatePropertyGetter(typeof(T), propertyName, out var propInfo);
           var setter = CreateSetMethod<T>(propertyName);
           return new PropertyAccessor<T>(propertyName,
              getter, setter, propInfo);
        }

        public IPropertyAccessor<T> GetPropertyAccessor<T>(String propertyName)
        {
           return PropertyDictionary<T>.Properties.GetOrAdd(propertyName,
              BuildPropertyAccessor<T>);
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
           VoidMethod? res;

            //super sophisticated
            var method = type.GetMethod(nameof(IList.Add));

            if (method != null)
            {
                if (method.GetParameters().Length > 1)
                {
                    method = GetExplicitAddMethod(type) ?? method;
                }

                #if GENERATECODE

                var dynam = CreateMethodCaller(method, true);
                res = (VoidMethod) dynam.CreateDelegate(typeof(VoidMethod));

                #else
                res = CreateMethodCaller(method);

                #endif
            }
            else res = default;

            return res!;
        }

        #if NET40 || NET45

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

        private static MethodInfo? GetExplicitAddMethod(Type type)
        {
            var allMyNonPublics = type.GetMethods(BindingFlags.Instance | 
                                                  BindingFlags.NonPublic | 
                                                  BindingFlags.FlattenHierarchy);

            foreach (var m in allMyNonPublics)
            {
                //Debug.WriteLine("yo method " + m.Name);

        if (!m.Name.EndsWith(nameof(IList.Add), StringComparison.Ordinal))
            continue;

                //if (!m.Name.Equals(nameof(IList.Add)))
                //    continue;

                if (m.GetParameters().Length != 1)
                    continue;

                return m;
            }

            return default;

        }

        private static MethodInfo? FindInvocableMethodImpl(Type type,
                                                           ICollection<String> possibleMethodNames,
                                                           Type[] paramTypes,
                                                           HashSet<Type> typeSearch)
        {
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

        // ReSharper disable once MemberCanBeMadeStatic.Local
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
            }
            #endif

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
                yield return declaringType.GetPropertyOrDie(propName);
                yield break;
            }

            var subPropTokens = propName.Split('.');
            var propInfo = declaringType.GetPropertyOrDie(subPropTokens[0]);
            yield return propInfo;

            for (var c = 1; c < subPropTokens.Length; c++)
            {
                propInfo = propInfo.PropertyType.GetPropertyOrDie(subPropTokens[c]);
                yield return propInfo;
            }
        }

       

        private const BindingFlags InterfaceMethodBindings = BindingFlags.Instance |
                                                             BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Object _lockNewType;
        private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownSensitive;

        private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownGeneric;


        

        private static readonly Dictionary<Type, TypePropertiesAccessor> _cachedTypeAccessors;
        private static readonly DoubleDictionary<Type, String, IPropertyAccessor> _cachedPropertyAccessors;

        //private static readonly Dictionary<>

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
