﻿using System;
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
        private static readonly ConcurrentDictionary<Type, Func<Object>> Constructors;
        
        private static readonly ConcurrentDictionary<Type, Boolean> KnownOnDeserialize;
        private readonly ITypeInferrer _typeInferrer;
        private readonly ITypeManipulator _typeManipulator;
        private readonly Dictionary<Type, Type> _typeSurrogates;
        private readonly IObjectManipulator _objectManipulator;
        private readonly IDynamicTypes _dynamicTypes;

        static ObjectInstantiator()
        {
            InstantionTypes = new ConcurrentDictionary<Type, InstantiationTypes>();
            Constructors = new ConcurrentDictionary<Type, Func<Object>>();
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

            return type.GetConstructor(argTypes);
        }

        public Delegate GetConstructorDelegate(Type type, Type delegateType)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (delegateType == null)
                throw new ArgumentNullException(nameof(delegateType));

            var genericArguments = delegateType.GetGenericArguments();
            var constructor = GetConstructor(type, genericArguments, out var argTypes);

            if (constructor == null)
            {
                throw new InvalidProgramException(
                    $"Type '{type.Name}' doesn't have the requested constructor.");
            }

            var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, argTypes, type);
            var ilGen = dynamicMethod.GetILGenerator();
            for (var i = 0; i < argTypes.Length; i++)
                ilGen.Emit(OpCodes.Ldarg, i);

            ilGen.Emit(OpCodes.Newobj, constructor);
            ilGen.Emit(OpCodes.Ret);
            return dynamicMethod.CreateDelegate(delegateType);
        }


        public Func<Object> GetConstructorDelegate(Type type)
        {
            var delType = typeof(Func<>).MakeGenericType(type);

            return (Func<Object>) GetConstructorDelegate(type, delType);
        }

        public Func<T> GetConstructorDelegate<T>()
            => (Func<T>) GetConstructorDelegate(typeof(T), typeof(Func<T>));

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
            if (!Constructors.TryGetValue(type, out var constructor))
            {

                constructor = GetConstructorDelegate(type);
                Constructors.TryAdd(type, constructor);
            }

            return constructor;
        }

        public Func<T> GetDefaultConstructor<T>() where T: class
        {
            var type = typeof(T);
            if (!Constructors.TryGetValue(type, out var constructor))
            {
                constructor = GetConstructorDelegate<T>();
                Constructors.TryAdd(type, constructor);
            }

            return constructor as Func<T>;
        }


        private Object CreateInstanceCacheConstructor(Type type)
        {
            if (Constructors.TryGetValue(type, out var constructor))
                return constructor();

            if (IsAnonymousType(type))
                return CreateInstanceDetectConstructor(type);

            constructor = GetConstructorDelegate(type);
            Constructors.TryAdd(type, constructor);
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