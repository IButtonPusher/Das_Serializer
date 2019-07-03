using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
//using Das.Extensions;
using Das.Types;
using Serializer.Core;

namespace Das.Serializer
{
    public class ObjectInstantiator : TypeCore, IInstantiator
    {
        private readonly ConcurrentDictionary<Type, InstantiationTypes> InstantionTypes;
        private readonly ConcurrentDictionary<Type, Func<object>> Constructors;
        private readonly ITypeInferrer _typeInferrer;
        private readonly ITypeManipulator _typeManipulator;
        private readonly IDictionary<Type, Type> _typeSurrogates;
        private readonly IObjectManipulator _objectManipulator;
        private readonly IDynamicTypes _dynamicTypes;
        private readonly ConcurrentDictionary<Type, ConstructorInfo> CachedConstructors;

        public ObjectInstantiator(ITypeInferrer typeInferrer, ITypeManipulator typeManipulator,
            IDictionary<Type, Type> typeSurrogates, IObjectManipulator objectManipulator,
            IDynamicTypes dynamicTypes)
            : base(typeManipulator.Settings)
        {
            _typeInferrer = typeInferrer;
            _typeManipulator = typeManipulator;
            _typeSurrogates = typeSurrogates;
            _objectManipulator = objectManipulator;
            _dynamicTypes = dynamicTypes;
            InstantionTypes = new ConcurrentDictionary<Type, InstantiationTypes>();
            Constructors = new ConcurrentDictionary<Type, Func<object>>();
            CachedConstructors = new ConcurrentDictionary<Type, ConstructorInfo>();
        }

        public object BuildDefault(Type type, Boolean isCacheConstructors)
        {
            if (!_typeSurrogates.TryGetValue(type, out var typ))
                typ = type;

            var instType = GetInstantiationType(typ);

            switch (instType)
            {
                case InstantiationTypes.EmptyString:
                    return String.Empty;
                case InstantiationTypes.DefaultConstructor:
                    return Activator.CreateInstance(typ);
                case InstantiationTypes.Emit:
                    return CreateInstance(typ, isCacheConstructors);
                case InstantiationTypes.EmptyArray:
                    var germane = _typeInferrer.GetGermaneType(typ);
                    return Array.CreateInstance(germane, 0);
                case InstantiationTypes.Uninitialized:
                    return FormatterServices.GetUninitializedObject(typ);
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
            }
            
            return null;
        }
       

        public T BuildDefault<T>(Boolean isCacheConstructors)
        {
            var def = BuildDefault(typeof(T), isCacheConstructors);
            return (T)def;
        }

        private static ConstructorInfo GetConstructor(Type type, IList<Type> genericArguments,
            out Type[] argTypes)
        {
            argTypes = genericArguments.Count > 1 ?
                genericArguments.Take(genericArguments.Count - 1).ToArray()
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

        public Func<object> GetConstructorDelegate(Type type) 
            => (Func<object>) GetConstructorDelegate(type, typeof(Func<object>));

        public void OnDeserialized(object obj, SerializationDepth depth)
        {
            if (obj == null)
                return;
            var str = _typeManipulator.GetStructure(obj.GetType(), depth);
            str.OnDeserialized(obj, _objectManipulator);
        }

        public bool TryGetPropertiesConstructor(Type type, out ConstructorInfo constr)
        {
            constr = null;
            var isAnomymous = IsAnonymousType(type);

            if (!isAnomymous && CachedConstructors.TryGetValue(type, out constr))
                return constr != null;

            var rProps = new Dictionary<string, Type>(
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

        public T CreatePrimitiveObject<T>(byte[] rawValue, Type objType)
        {
            if (rawValue.Length == 0)
                return default(T);

            var handle = GCHandle.Alloc(rawValue, GCHandleType.Pinned);
            var structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), objType);
            handle.Free();
            return structure;
        }

        public Object CreatePrimitiveObject(byte[] rawValue, Type objType)
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

        private object CreateInstance(Type type, Boolean isCacheTypeConstructors)
        {
            if (!isCacheTypeConstructors  || IsAnonymousType(type))
            {
                var ctor = GetConstructor(type, new List<Type>(), out _);
                var ctored = ctor.Invoke(new Object[0]);
                return ctored;
            }

            if (Constructors.TryGetValue(type, out var constructor))
                return constructor();

            constructor = GetConstructorDelegate(type);
            Constructors.TryAdd(type, constructor);
            return constructor();
        }
    }
}
