using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Das.Serializer.Types;

namespace Das.Serializer
{
    public abstract class BaseTypeManipulator : TypeCore, 
                                                ITypeManipulator
    {
        protected BaseTypeManipulator(ISerializerSettings settings, 
                                      INodePool nodePool) : base(settings)
        {
            _nodePool = nodePool;
        }

        static BaseTypeManipulator()
        {
            _lockNewType = new Object();
            _knownSensitive = new ConcurrentDictionary<Type, ITypeStructure>();
            _knownInsensitive = new ConcurrentDictionary<Type, ITypeStructure>();
            _cachedPropertyAccessors = new DoubleDictionary<Type, string, IPropertyAccessor>();
        }

        public override Boolean HasSettableProperties(Type type)
        {
            if (_knownSensitive.TryGetValue(type, out var result) &&
                result.Depth >= SerializationDepth.GetSetProperties)
                return result.PropertyCount > 0;

            return base.HasSettableProperties(type);
        }

        public abstract Func<object, object> CreateFieldGetter(FieldInfo fieldInfo);

        public abstract Action<object, object?> CreateFieldSetter(FieldInfo fieldInfo);

        public abstract Func<object, object[], object> CreateFuncCaller(MethodInfo method);

        public abstract Func<object, object> CreatePropertyGetter(Type targetType, 
                                                                  PropertyInfo propertyInfo);

        public abstract Func<object, object> CreatePropertyGetter(Type targetType, 
                                                                  String propertyName);

        public abstract PropertySetter CreateSetMethod(MemberInfo memberInfo);

        public abstract PropertySetter? CreateSetMethod(Type declaringType, 
                                                        String memberName);

        public abstract VoidMethod? GetAdder(Type collectionType, 
                                             Object exampleValue);

        public abstract VoidMethod GetAdder(IEnumerable collection, 
                                            Type? collectionType = null);

        public abstract MethodInfo? GetAddMethod<T>(IEnumerable<T> collection);

        public abstract MethodInfo GetAddMethod(Type collectionType);

        public IEnumerable<MethodInfo> GetInterfaceMethods(Type type)
        {
            foreach (var parentInterface in type.GetInterfaces())
            foreach (var pp in GetInterfaceMethods(parentInterface))
                yield return pp;

            foreach (var mi in type.GetMethods(InterfaceMethodBindings))
            {
                if (mi.IsPrivate)
                    continue;
                yield return mi;
            }
        }

        public IEnumerable<INamedField> GetPropertiesToSerialize(Type type,
                                                                 ISerializationDepth depth)
        {
            var str = GetTypeStructure(type, depth);
            foreach (var pi in str.GetMembersToSerialize(depth))   
                yield return pi;
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

        public ITypeStructure GetTypeStructure(Type type, ISerializationDepth depth)
        {
            if (Settings.IsPropertyNamesCaseSensitive)
                return ValidateCollection(type, depth, true);

            return ValidateCollection(type, depth, false);
        }

        public static IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type)
        {
            while (true)
            {
                foreach (var field in type.GetFields(Const.NonPublic))
                    yield return field;

                var parent = type.BaseType;
                if (parent == null) yield break;

                type = parent;
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

        public abstract bool TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo, out Action<object, object?> setter);

        public abstract bool TryGetAddMethod(Type collectionType, out MethodInfo addMethod);

        public abstract VoidMethod CreateMethodCaller(MethodInfo method);

        private ITypeStructure ValidateCollection(Type type, 
                                                  ISerializationDepth depth,
                                                  Boolean caseSensitive)
        {
            var collection = caseSensitive ? _knownSensitive : _knownInsensitive;

            var doCache = Settings.CacheTypeConstructors;


            if (IsAlreadyExists(type, doCache, depth, collection, out var result))
                return result;

            var pool = _nodePool;

            lock (_lockNewType)
            {
                if (IsAlreadyExists(type, doCache, depth, collection, out result))
                    return result;

                result = new TypeStructure(type, caseSensitive, depth, this, pool);
                if (!doCache)
                    return result;

                return collection.AddOrUpdate(type, result, (k, v) => v.Depth > result.Depth ? v : result);
            }
        }

        private static Boolean IsAlreadyExists(Type type, 
                                               Boolean doCache, 
                                               ISerializationDepth depth,
                                               ConcurrentDictionary<Type, ITypeStructure> collection,
                                               out ITypeStructure res)
        {
            res = default!;

            if (!doCache || !collection.TryGetValue(type, out res)) 
                return false;

            if (res.Depth < depth.SerializationDepth) return false;


            if (doCache && collection.TryGetValue(type, out res) &&
                res.Depth >= depth.SerializationDepth)
                return true;

            res = default!;
            return false;
        }

        private static readonly Object _lockNewType; 
        private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownSensitive;
        private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownInsensitive;
        private static readonly DoubleDictionary<Type, String, IPropertyAccessor> _cachedPropertyAccessors;
        private readonly INodePool _nodePool;

        private const BindingFlags InterfaceMethodBindings = BindingFlags.Instance |
                                                             BindingFlags.Public | BindingFlags.NonPublic;


        public IPropertyAccessor GetPropertyAccessor(Type declaringType, 
                                                     String propertyName)
        {
            lock (_lockNewType)
            {
                if (_cachedPropertyAccessors.TryGetValue(declaringType, propertyName, out var accessor))
                    return accessor;
                
                var getter = CreatePropertyGetter(declaringType, propertyName);
                var setter = CreateSetMethod(declaringType, propertyName);
                accessor = new SimplePropertyAccessor(declaringType, propertyName, getter, setter);

                _cachedPropertyAccessors.Add(declaringType, propertyName, accessor);
                
                return accessor;
            }
        }

        protected static MemberInfo GetMemberOrDie(Type declaringType,
                                                   String propName)
        {
            var membersOnly = GetMembersOrDie(declaringType, propName);
            if (membersOnly.Length != 1)
                throw new AmbiguousMatchException(nameof(propName));

            return membersOnly[0];
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
    }
}