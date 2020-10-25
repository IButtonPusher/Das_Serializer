using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer;

namespace Das.Types
{
    public class TypeManipulator : BaseTypeManipulator, ITypeManipulator
    {
        //static TypeManipulator()
        //{
        //    _lockNewType = new Object();
        //    _knownSensitive = new ConcurrentDictionary<Type, ITypeStructure>();
        //    _knownInsensitive = new ConcurrentDictionary<Type, ITypeStructure>();
        //}

        public TypeManipulator(ISerializerSettings settings, INodePool nodePool)
            : base(settings, nodePool)
        {
           // _nodePool = nodePool;
            _cachedAdders = new ConcurrentDictionary<Type, VoidMethod>();
        }

        /// <summary>
        ///     Returns a delegate that can be invoked to quickly get the value for an object
        ///     of targetType
        /// </summary>
        public sealed override Func<Object, Object> CreatePropertyGetter(Type targetType,
                                                         PropertyInfo propertyInfo)
        {
            var setParamType = Const.ObjectType;
            Type[] setParamTypes = {setParamType};
            var setReturnType = Const.ObjectType;

            var owner = typeof(DasType);

            var getMethod = new DynamicMethod(String.Empty, setReturnType,
                setParamTypes, owner, true);

            var il = getMethod.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);

            il.Emit(targetType.IsValueType
                ? OpCodes.Unbox
                : OpCodes.Castclass, targetType);

            var targetGetMethod = propertyInfo.GetGetMethod();
            var opCode = targetType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
            il.Emit(opCode, targetGetMethod!);
            var returnType = targetGetMethod.ReturnType;


            if (returnType.IsValueType) il.Emit(OpCodes.Box, returnType);

            il.Emit(OpCodes.Ret);

            var del = getMethod.CreateDelegate(Expression.GetFuncType(setParamType, setReturnType));
            return (Func<Object, Object>)del;
        }


        public sealed override PropertySetter CreateSetMethod(MemberInfo memberInfo)
        {
            return CreateSetMethodImpl(memberInfo);
        }

        IEnumerable<FieldInfo> ITypeManipulator.GetRecursivePrivateFields(Type type)
        {
            return GetRecursivePrivateFields(type);
        }

        public sealed override Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
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

        public sealed override Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo)
        {
            var dynam = new DynamicMethod(String.Empty, Const.ObjectType, Const.SingleObjectTypeArray
                , typeof(Func<Object, Object>), true);

            var il = dynam.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
            }
            else
            {
                il.Emit(OpCodes.Ldsfld, fieldInfo);
            }

            if (fieldInfo.FieldType.IsValueType) il.Emit(OpCodes.Box, fieldInfo.FieldType);

            il.Emit(OpCodes.Ret);
            return (Func<Object, Object>) dynam.CreateDelegate(typeof(Func<Object, Object>));
        }

        public sealed override Action<Object, Object?> CreateFieldSetter(FieldInfo fieldInfo)
        {
            var dynam = new DynamicMethod(
                String.Empty
                , typeof(void)
                , Const.TwoObjectTypeArray
                , typeof(VoidMethod)
                , true
            );

            var il = dynam.GetILGenerator();

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldarg_1);

            if (fieldInfo.FieldType.IsValueType) il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Stfld, fieldInfo);
            else
                il.Emit(OpCodes.Stsfld, fieldInfo);


            il.Emit(OpCodes.Ret);
            return (Action<Object, Object?>) dynam.CreateDelegate(typeof(Action<Object, Object?>));
        }

        public sealed override Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method)
        {
            var dyn = CreateMethodCaller(method, false);
            return (Func<Object, Object[], Object>) dyn.CreateDelegate(typeof(Func<Object, Object[], Object>));
        }


        public sealed override VoidMethod? GetAdder(Type collectionType, 
                                                    Object exampleValue)
        {
            if (_cachedAdders.TryGetValue(collectionType, out var res))
                return res;

            var eType = exampleValue.GetType();

            var addMethod = collectionType.GetMethod("Add", new[] {eType});

            if (addMethod == null)
            {
                var interfaces = (from i in collectionType.GetInterfaces()
                    let args = i.GetGenericArguments()
                    where args.Length == 1
                          && args[0] == eType
                    select i).FirstOrDefault();

                if (interfaces != null)
                    addMethod = interfaces.GetMethod("Add", new[] {eType});
            }

            if (addMethod == null)
                return default;

            return CreateMethodCaller(addMethod);
        }


        /// <summary>
        ///     Gets a delegate to add an object to a non-generic collection
        /// </summary>
        public sealed override VoidMethod GetAdder(IEnumerable collection, Type? type = null)
        {
            if (type == null)
                type = collection.GetType();

            if (_cachedAdders.TryGetValue(type, out var res))
                return res;


            #if NET40 || NET45
            if (type.IsGenericType)
            {
                dynamic dCollection = collection;
                res = CreateAddDelegate(dCollection);
                return res;
                //no need to cache here since it will be added to the cache in the other method
            }
            #endif

            if (collection is ICollection icol)
            {
                res = CreateAddDelegate(icol, type);
                return res;
            }

            var boxList = new List<Object>(collection.OfType<Object>());
            res = CreateAddDelegate(boxList, type);


            return res;
        }

        /// <summary>
        ///     Detects the Add, Enqueue, Push etc method for generic collections
        /// </summary>
        public sealed override MethodInfo? GetAddMethod<T>(IEnumerable<T> collection)
        {
            var cType = collection.GetType();

            if (typeof(ICollection<T>).IsAssignableFrom(cType))
                return typeof(ICollection<T>).GetMethod(nameof(ICollection<T>.Add), new[] {typeof(T)})!;

            if (typeof(IList).IsAssignableFrom(cType))
                return typeof(IList).GetMethod(nameof(IList.Add), new[] {typeof(T)})!;

            var prmType = cType.GetGenericArguments().FirstOrDefault()
                          ?? Const.ObjectType;

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

        public sealed override MethodInfo GetAddMethod(Type cType)
        {
            var adder = GetAddMethodImpl(cType);
            return adder ?? throw new MissingMethodException(cType.FullName, "Add");
        }

        public sealed override Boolean TryGetAddMethod(Type collectionType, out MethodInfo addMethod)
        {
            addMethod = GetAddMethodImpl(collectionType)!;
            return addMethod != null;
        }

        //public Type? GetPropertyType(Type classType, String propName)
        //{
        //    var ts = GetTypeStructure(classType, DepthConstants.AllProperties);
        //    return ts.MemberTypes.TryGetValue(propName, out var res) ? res.Type : default;
        //}

        //public IEnumerable<INamedField> GetPropertiesToSerialize(Type type,
        //                                                         ISerializationDepth depth)
        //{
        //    var str = GetTypeStructure(type, depth);
        //    foreach (var pi in str.GetMembersToSerialize(depth))
        //        yield return pi;
        //}

        //public Type InstanceMemberType(MemberInfo info)
        //{
        //    switch (info)
        //    {
        //        case PropertyInfo prop:
        //            return prop.PropertyType;
        //        case FieldInfo field:
        //            return field.FieldType;
        //        default:
        //            throw new InvalidOperationException();
        //    }
        //}

        //public IEnumerable<MethodInfo> GetInterfaceMethods(Type type)
        //{
        //    foreach (var parentInterface in type.GetInterfaces())
        //    foreach (var pp in GetInterfaceMethods(parentInterface))
        //        yield return pp;

        //    foreach (var mi in type.GetMethods(InterfaceMethodBindings))
        //    {
        //        if (mi.IsPrivate)
        //            continue;
        //        yield return mi;
        //    }
        //}

        //public override Boolean HasSettableProperties(Type type)
        //{
        //    if (_knownSensitive.TryGetValue(type, out var result) &&
        //        result.Depth >= SerializationDepth.GetSetProperties)
        //        return result.PropertyCount > 0;

        //    return base.HasSettableProperties(type);
        //}

        //public ITypeStructure GetStructure<T>(ISerializationDepth depth)
        //{
        //    return GetTypeStructure(typeof(T), depth);
        //}

        //public ITypeStructure GetTypeStructure(Type type, ISerializationDepth depth)
        //{
        //    if (Settings.IsPropertyNamesCaseSensitive)
        //        return ValidateCollection(type, depth, true);

        //    return ValidateCollection(type, depth, false);
        //}

        /// <summary>
        ///     Gets a delegate to add an object to a generic collection
        /// </summary>
        public VoidMethod CreateAddDelegate<T>(IEnumerable<T> collection)
        {
            var colType = collection.GetType();

            if (_cachedAdders.TryGetValue(colType, out var res))
                return res;

            var method = GetAddMethod(collection);
            if (method != null)
            {
                var dynam = CreateMethodCaller(method, true);
                res = (VoidMethod) dynam.CreateDelegate(typeof(VoidMethod));
            }

            _cachedAdders.TryAdd(colType, res!);

            return res!;
        }


        private VoidMethod CreateAddDelegate(ICollection collection, Type type)
        {
            var colType = collection.GetType();

            if (_cachedAdders.TryGetValue(colType, out var res))
                return res;

            //super sophisticated
            var method = type.GetMethod(nameof(IList.Add));
            if (method != null)
            {
                var dynam = CreateMethodCaller(method, true);
                res = (VoidMethod) dynam.CreateDelegate(typeof(VoidMethod));
            }

            _cachedAdders.TryAdd(colType, res!);

            return res!;
        }


        public sealed override VoidMethod CreateMethodCaller(MethodInfo method)
        {
            var dyn = CreateMethodCaller(method, true);
            return (VoidMethod) dyn.CreateDelegate(typeof(VoidMethod));
        }

        public static DynamicMethod CreateMethodCaller(MethodInfo method, Boolean isSuppressReturnValue)
        {
            Type[] argTypes = {Const.ObjectType, typeof(Object[])};
            var parms = method.GetParameters();

            var retType = isSuppressReturnValue ? typeof(void) : Const.ObjectType;

            var dynam = new DynamicMethod(String.Empty, retType, argTypes
                , typeof(DasType), true);
            var il = dynam.GetILGenerator();

            //pass the target object.  If it's a struct (value type) we have to pass the address
            il.Emit(method.DeclaringType?.IsValueType == true ? OpCodes.Ldarga : OpCodes.Ldarg, 0);

            for (var i = 0; i < parms.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);

                var parmType = parms[i].ParameterType;
                if (parmType.IsValueType) il.Emit(OpCodes.Unbox_Any, parmType);
            }

            il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);

            if (method.ReturnType != typeof(void))
            {
                if (!isSuppressReturnValue)
                {
                    if (method.ReturnType.IsValueType) il.Emit(OpCodes.Box, method.ReturnType);
                }
                else
                {
                    il.Emit(OpCodes.Pop);
                }
            }

            il.Emit(OpCodes.Ret);

            return dynam;
        }

        /// <summary>
        ///     Returns a delegate that can be invoked to quickly set the value for an object
        ///     of targetType.  This method assumes this property has a setter. For properties
        ///     without a setter use CreateReadOnlyPropertySetter
        /// </summary>
        private static PropertySetter CreateSetMethodImpl(MemberInfo memberInfo)
        {
            Type paramType;
            switch (memberInfo)
            {
                case PropertyInfo info:
                    paramType = info.PropertyType;
                    break;
                case FieldInfo info:
                    paramType = info.FieldType;
                    break;
                default:
                    throw new Exception("Can only create set methods for properties and fields.");
            }

            var reflectedType = memberInfo.ReflectedType;
            var decType = memberInfo.DeclaringType;
            if (reflectedType == null || decType == null)
                throw new InvalidOperationException();

            var setter = new DynamicMethod(
                String.Empty,
                typeof(void),
                ParamTypes,
                reflectedType.Module,
                true);
            var generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldind_Ref);

            if (decType.IsValueType) generator.Emit(OpCodes.Unbox, decType);

            generator.Emit(OpCodes.Ldarg_1);
            if (paramType.IsValueType)
                generator.Emit(OpCodes.Unbox_Any, paramType);

            switch (memberInfo)
            {
                case PropertyInfo info:
                    generator.Emit(OpCodes.Callvirt, info.GetSetMethod(true)!);
                    break;
                case FieldInfo field:
                    generator.Emit(OpCodes.Stfld, field);
                    break;
            }

            generator.Emit(OpCodes.Ret);

            return (PropertySetter) setter.CreateDelegate(typeof(PropertySetter));
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

        //public static IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type)
        //{
        //    while (true)
        //    {
        //        foreach (var field in type.GetFields(Const.NonPublic))
        //            yield return field;

        //        var parent = type.BaseType;
        //        if (parent == null) yield break;

        //        type = parent;
        //    }
        //}

        //private ITypeStructure ValidateCollection(Type type, 
        //                                          ISerializationDepth depth,
        //                                          Boolean caseSensitive)
        //{
        //    var collection = caseSensitive ? _knownSensitive : _knownInsensitive;

        //    var doCache = Settings.CacheTypeConstructors;


        //    if (IsAlreadyExists(type, doCache, depth, collection, out var result))
        //        return result;

        //    var pool = _nodePool;

        //    lock (_lockNewType)
        //    {
        //        if (IsAlreadyExists(type, doCache, depth, collection, out result))
        //            return result;

        //        result = new TypeStructure(type, caseSensitive, depth, this, pool);
        //        if (!doCache)
        //            return result;

        //        return collection.AddOrUpdate(type, result, (k, v) => v.Depth > result.Depth ? v : result);
        //    }
        //}

        //private static Boolean IsAlreadyExists(Type type, 
        //                                       Boolean doCache, 
        //                                       ISerializationDepth depth,
        //                                       ConcurrentDictionary<Type, ITypeStructure> collection,
        //                                       out ITypeStructure res)
        //{
        //    res = default!;

        //    if (!doCache || !collection.TryGetValue(type, out res)) 
        //        return false;

        //    if (res.Depth < depth.SerializationDepth) return false;


        //    if (doCache && collection.TryGetValue(type, out res) &&
        //        res.Depth >= depth.SerializationDepth)
        //        return true;

        //    res = default!;
        //    return false;
        //}

        //private const BindingFlags InterfaceMethodBindings = BindingFlags.Instance |
        //                                                     BindingFlags.Public | BindingFlags.NonPublic;


        //private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownSensitive;
        //private static readonly ConcurrentDictionary<Type, ITypeStructure> _knownInsensitive;

        private static readonly Type[] ParamTypes =
        {
            Const.ObjectType.MakeByRefType(), Const.ObjectType
        };


        //private static readonly Object _lockNewType;

        private readonly ConcurrentDictionary<Type, VoidMethod> _cachedAdders;
        //private readonly INodePool _nodePool;
    }
}