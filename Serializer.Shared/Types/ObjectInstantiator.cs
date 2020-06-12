using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Das.Types;

namespace Das.Serializer
{
    public class ObjectInstantiator : TypeCore, IInstantiator
    {
        private static readonly ConcurrentDictionary<Type, InstantiationTypes> InstantionTypes;
        private static readonly ConcurrentDictionary<Type, Func<Object>> ConstructorDelegates;
        private static readonly ConcurrentDictionary<Type, ConstructorInfo> Constructors;
        
        private static readonly ConcurrentDictionary<Type, Boolean> KnownOnDeserialize;
        private readonly ITypeInferrer _typeInferrer;
        private readonly ITypeManipulator _typeManipulator;
        private readonly Dictionary<Type, Type> _typeSurrogates;
        private readonly IObjectManipulator _objectManipulator;
        private readonly IDynamicTypes _dynamicTypes;

        static ObjectInstantiator()
        {
            InstantionTypes = new ConcurrentDictionary<Type, InstantiationTypes>();
            ConstructorDelegates = new ConcurrentDictionary<Type, Func<Object>>();
            Constructors = new ConcurrentDictionary<Type, ConstructorInfo>();
            KnownOnDeserialize = new ConcurrentDictionary<Type, Boolean>();
        }


        public ObjectInstantiator(ITypeInferrer typeInferrer, ITypeManipulator typeManipulator,
            IDictionary<Type, Type> typeSurrogates, IObjectManipulator objectManipulator,
            IDynamicTypes dynamicTypes)
            : base(typeManipulator.Settings)
        {
            _typeInferrer = typeInferrer;
            _typeManipulator = typeManipulator;
            _typeSurrogates = new Dictionary<Type, Type>(typeSurrogates);
            _objectManipulator = objectManipulator;
            _dynamicTypes = dynamicTypes;
            
        }

        public Object BuildDefault(Type type, Boolean isCacheConstructors)
        {
            if (_typeSurrogates.ContainsKey(type))
                type = _typeSurrogates[type];

            var instType = GetInstantiationType(type);

            switch (instType)
            {
                case InstantiationTypes.EmptyString:
                    return String.Empty;
                case InstantiationTypes.DefaultConstructor:
                    return Activator.CreateInstance(type);
                case InstantiationTypes.Emit:
                    if (isCacheConstructors)
                        return CreateInstanceCacheConstructor(type);
                    else
                        return CreateInstanceDetectConstructor(type);
                    
                case InstantiationTypes.EmptyArray:
                    var germane = _typeInferrer.GetGermaneType(type);
                    return Array.CreateInstance(germane, 0);
                case InstantiationTypes.Uninitialized:
                    return FormatterServices.GetUninitializedObject(type);
                case InstantiationTypes.NullObject:
                    return null;
                case InstantiationTypes.Abstract:
                    switch (Settings.NotFoundBehavior)
                    {
                        case TypeNotFound.GenerateRuntime:
                            var dynamicType = _dynamicTypes.GetDynamicImplementation(type);
                            return Activator.CreateInstance(dynamicType);
                        case TypeNotFound.ThrowException:
                            throw new TypeLoadException(type.Name);
                        case TypeNotFound.NullValue:
                            return null;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }


        public T BuildDefault<T>(Boolean isCacheConstructors)
        {
            var def = BuildDefault(typeof(T), isCacheConstructors);
            return (T) def;
        }

        private static ConstructorInfo GetConstructor(Type type, IList<Type> genericArguments,
            out Type[] argTypes)
        {
            argTypes = genericArguments.Count > 1
                ? genericArguments.Take(genericArguments.Count - 1).ToArray()
                : Type.EmptyTypes;

            var ctor = type.GetConstructor(Const.AnyInstance, null, argTypes, null);
            return ctor;
        }

        public TDelegate GetConstructorDelegate<TDelegate>(Type type)
        where TDelegate : Delegate
        {
            if (TryGetConstructorDelegate<TDelegate>(type, out var res))
                return res;

            throw new InvalidProgramException(
                $"Type '{type.Name}' doesn't have the requested constructor.");
        }

        public bool TryGetConstructorDelegate<TDelegate>(Type type, out TDelegate result)
            where TDelegate : Delegate
        {
            if (TryGetConstructorDelegate(type, typeof(TDelegate), out var maybe)
                && (maybe is TDelegate td))
            {
                result = td;
                return true;
            }

            result = default;
            return false;

            //if (type == null)
            //    throw new ArgumentNullException(nameof(type));

            //var delegateType = typeof(TDelegate);

            //if (delegateType == null)
            //    throw new ArgumentNullException(nameof(delegateType));

            //var genericArguments = delegateType.GetGenericArguments();
            //var constructor = GetConstructor(type, genericArguments, out var argTypes);

            //if (constructor == null)
            //{
            //    result = default!;
            //    return false;
            //}

            //var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, argTypes, type);
            //var ilGen = dynamicMethod.GetILGenerator();
            //for (var i = 0; i < argTypes.Length; i++)
            //    ilGen.Emit(OpCodes.Ldarg, i);

            //ilGen.Emit(OpCodes.Newobj, constructor);
            //ilGen.Emit(OpCodes.Ret);
            //result = (TDelegate)dynamicMethod.CreateDelegate(delegateType);
            //return true;
        }

        public bool TryGetConstructorDelegate(Type type, Type delegateType, out Delegate result)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (delegateType == null)
                throw new ArgumentNullException(nameof(delegateType));

            var genericArguments = delegateType.GetGenericArguments();
            var constructor = GetConstructor(type, genericArguments, out var argTypes);

            if (constructor == null)
            {
                result = default!;
                return false;
            }

            var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, argTypes, type);
            var ilGen = dynamicMethod.GetILGenerator();
            for (var i = 0; i < argTypes.Length; i++)
                ilGen.Emit(OpCodes.Ldarg, i);

            ilGen.Emit(OpCodes.Newobj, constructor);
            ilGen.Emit(OpCodes.Ret);
            result = dynamicMethod.CreateDelegate(delegateType);
            return true;
        }




        public Func<Object> GetConstructorDelegate(Type type)
        {
            var delType = typeof(Func<>).MakeGenericType(type);

            if (TryGetConstructorDelegate(type, delType, out var res))
                return (Func<Object>)res;

            throw new InvalidProgramException(
                $"Type '{type.Name}' doesn't have the requested constructor.");
            //return (Func<Object>) GetConstructorDelegate(type, delType);
        }

        public Func<T> GetConstructorDelegate<T>()
            => GetConstructorDelegate<Func<T>>(typeof(T));

        public void OnDeserialized(IValueNode node, ISerializationDepth depth)
        {
            var wasKnown = KnownOnDeserialize.TryGetValue(node.Type, out var dothProceed);

            if (wasKnown && !dothProceed)
                    return;
            
            var str = _typeManipulator.GetTypeStructure(node.Type, depth);
            dothProceed = str.OnDeserialized(node.Value, _objectManipulator);
            if (!wasKnown)
                KnownOnDeserialize.TryAdd(node.Type, dothProceed);
        }


      

        public T CreatePrimitiveObject<T>(Byte[] rawValue, Type objType)
        {
            if (rawValue.Length == 0)
                return default;

            var handle = GCHandle.Alloc(rawValue, GCHandleType.Pinned);
            var structure = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), objType);
            handle.Free();
            return structure;
        }

        public Object CreatePrimitiveObject(Byte[] rawValue, Type objType)
            => CreatePrimitiveObject<Object>(rawValue, objType);

        private InstantiationTypes GetInstantiationType(Type type)
        {
            if (InstantionTypes.TryGetValue(type, out var res))
                return res;
            if (type == typeof(String))
                res = InstantiationTypes.EmptyString;
            else if (type.IsArray)
                res = InstantiationTypes.EmptyArray;
            else if (!type.IsAbstract)
            {
                if (_typeInferrer.HasEmptyConstructor(type))
                {
                    if (_typeInferrer.IsCollection(type))
                        res = InstantiationTypes.DefaultConstructor;
                    else
                        res = InstantiationTypes.Emit;
                }
                else if (type.IsGenericType)
                    res = InstantiationTypes.NullObject; //Nullable<T>
                else
                    res = InstantiationTypes.Uninitialized;
            }
            else if (_typeInferrer.IsCollection(type))
                res = InstantiationTypes.EmptyArray;
            else return InstantiationTypes.Abstract;

            InstantionTypes.TryAdd(type, res);
            return res;
        }

        public Func<Object> GetDefaultConstructor(Type type)
        {
            if (!ConstructorDelegates.TryGetValue(type, out var constructor))
            {
                constructor = GetConstructorDelegate(type);
                ConstructorDelegates.TryAdd(type, constructor);
            }

            return constructor;
        }

        public bool TryGetDefaultConstructor(Type type, out ConstructorInfo ctor)
        {
            if (Constructors.TryGetValue(type, out ctor))
            {
                if (ctor == null)
                    return false;

                if (ctor.GetParameters().Length == 0)
                    return true;

                ctor = GetConstructor(type, new List<Type>(), out _);
                goto byeNow;
            }

            ctor = GetConstructor(type, new List<Type>(), out _);
            Constructors.TryAdd(type, ctor);

            byeNow:
            return ctor != null;
        }

        public bool TryGetDefaultConstructor<T>(out ConstructorInfo ctor)
        {
            var type = typeof(T);

            return TryGetDefaultConstructor(type, out ctor);
        }

        public bool TryGetDefaultConstructorDelegate<T>(out Func<T> res) where T : class
        {
            var type = typeof(T);
            if (ConstructorDelegates.TryGetValue(type, out var constructor))
            {
                res = constructor as Func<T>;
                return true;
            }

            if (!TryGetConstructorDelegate<Func<T>>(typeof(T), out var del))
            {
                res = default;
                return false;
            }

            constructor = (Func<Object>) del;
            ConstructorDelegates.TryAdd(type, constructor);

            res = constructor as Func<T>;
            return true;
        }

        public Func<T> GetDefaultConstructor<T>() where T: class
        {
            var type = typeof(T);
            if (!ConstructorDelegates.TryGetValue(type, out var constructor))
            {
                constructor = GetConstructorDelegate<T>();
                ConstructorDelegates.TryAdd(type, constructor);
            }

            return constructor as Func<T>;
        }


        private Object CreateInstanceCacheConstructor(Type type)
        {
            if (ConstructorDelegates.TryGetValue(type, out var constructor))
                return constructor();

            if (IsAnonymousType(type))
                return CreateInstanceDetectConstructor(type);

            constructor = GetConstructorDelegate(type);
            ConstructorDelegates.TryAdd(type, constructor);
            return constructor();
        }

        private Object CreateInstanceDetectConstructor(Type type)
        {
            var ctor = GetConstructor(type, new List<Type>(), out _);
            var ctored = ctor.Invoke(new Object[0]);
            return ctored;
        }
    }
}