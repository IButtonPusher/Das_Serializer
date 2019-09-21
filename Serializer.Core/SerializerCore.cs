using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Das;
using Das.Serializer;
using Das.Serializer.Objects;

namespace Serializer.Core
{
    public abstract class SerializerCore : TypeCore, ISerializationCore
    {
        private readonly IDynamicFacade _dynamicFacade;

        protected SerializerCore(IDynamicFacade dynamicFacade, ISerializerSettings settings)
            : base(settings)
        {
            _dynamicFacade = dynamicFacade;

            Surrogates = dynamicFacade.Surrogates is ConcurrentDictionary<Type, Type> conc
                ? conc
                : new ConcurrentDictionary<Type, Type>(Surrogates);
        }


        public ITextParser TextParser => _dynamicFacade.TextParser;

        public IDynamicTypes DynamicTypes => _dynamicFacade.DynamicTypes;

        public IInstantiator ObjectInstantiator => _dynamicFacade.ObjectInstantiator;

        public ITypeInferrer TypeInferrer => _dynamicFacade.TypeInferrer;

        public ITypeManipulator TypeManipulator => _dynamicFacade.TypeManipulator;
        public IAssemblyList AssemblyList => _dynamicFacade.AssemblyList;

        public IObjectManipulator ObjectManipulator => _dynamicFacade.ObjectManipulator;

        protected readonly ConcurrentDictionary<Type, Type> Surrogates;

        IDictionary<Type, Type> IDynamicFacade.Surrogates => Surrogates;

        public Object BuildDefault(Type type, Boolean isCacheConstructors)
            => ObjectInstantiator.BuildDefault(type, isCacheConstructors);

        public T BuildDefault<T>(Boolean isCacheConstructors)
            => ObjectInstantiator.BuildDefault<T>(isCacheConstructors);

        public Delegate GetConstructorDelegate(Type type, Type delegateType) =>
            ObjectInstantiator.GetConstructorDelegate(type, delegateType);

        public Func<Object> GetConstructorDelegate(Type type) => ObjectInstantiator.GetConstructorDelegate(type);

        public void OnDeserialized(Object obj, SerializationDepth depth)
        {
            ObjectInstantiator.OnDeserialized(obj, depth);
        }

        public Boolean TryGetPropertiesConstructor(Type type, out ConstructorInfo constr) =>
            ObjectInstantiator.TryGetPropertiesConstructor(type, out constr);

        public T CreatePrimitiveObject<T>(Byte[] rawValue, Type objType) =>
            ObjectInstantiator.CreatePrimitiveObject<T>(rawValue, objType);

        public Object CreatePrimitiveObject(Byte[] rawValue, Type objType) =>
            ObjectInstantiator.CreatePrimitiveObject(rawValue, objType);

        public Type GetTypeFromClearName(String clearName) => TypeInferrer.GetTypeFromClearName(clearName);

        public String ToClearName(Type type, Boolean isOmitAssemblyName) =>
            TypeInferrer.ToClearName(type, isOmitAssemblyName);

        public Type GetGermaneType(Type ownerType) => TypeInferrer.GetGermaneType(ownerType);

        public Type GetGermaneType(Object mustBeCollection)
            => TypeInferrer.GetGermaneType(mustBeCollection);

        public void ClearCachedNames()
        {
            TypeInferrer.ClearCachedNames();
        }

        public Int32 BytesNeeded(Type typ) => TypeInferrer.BytesNeeded(typ);

        public IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type)
            => TypeManipulator.GetRecursivePrivateFields(type);

        public Boolean IsDefaultValue(Object o) => TypeInferrer.IsDefaultValue(o);

        public String ToPropertyStyle(String name) => TypeInferrer.ToPropertyStyle(name);

        public VoidMethod GetAdder(IEnumerable collection, Type type = null) =>
            TypeManipulator.GetAdder(collection, type);

        public Func<Object, Object> CreatePropertyGetter(Type targetType, PropertyInfo propertyInfo) =>
            TypeManipulator.CreatePropertyGetter(targetType, propertyInfo);

        public Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
            out Action<Object, Object> setter)
            => TypeManipulator.TryCreateReadOnlyPropertySetter(propertyInfo, out setter);

        public Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo) =>
            TypeManipulator.CreateFieldGetter(fieldInfo);

        public Action<Object, Object> CreateFieldSetter(FieldInfo fieldInfo) =>
            TypeManipulator.CreateFieldSetter(fieldInfo);

        public VoidMethod CreateMethodCaller(MethodInfo method) => TypeManipulator.CreateMethodCaller(method);

        public Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method) =>
            TypeManipulator.CreateFuncCaller(method);

        public MethodInfo GetAddMethod<T>(IEnumerable<T> collection) => TypeManipulator.GetAddMethod(collection);

        public Type GetPropertyType(Type classType, String propName) =>
            TypeManipulator.GetPropertyType(classType, propName);

        public Int32 PropertyCount(Type type) => TypeManipulator.PropertyCount(type);

        public ITypeStructure GetStructure(Type type, SerializationDepth depth)
            => TypeManipulator.GetStructure(type, depth);

        public ITypeStructure GetStructure<T>(SerializationDepth depth)
            => TypeManipulator.GetStructure<T>(depth);

        public IEnumerable<MemberInfo> GetPropertiesToSerialize(Type type,
            SerializationDepth depth)
            => TypeManipulator.GetPropertiesToSerialize(type, depth);

        public Type InstanceMemberType(MemberInfo info) => TypeManipulator.InstanceMemberType(info);

        public IEnumerable<MethodInfo> GetInterfaceMethods(Type type) => TypeManipulator.GetInterfaceMethods(type);

        public NamedValueNode GetPropertyResult(Object o, Type asType, String propertyName) =>
            ObjectManipulator.GetPropertyResult(o, asType, propertyName);

        public T GetPropertyValue<T>(Object obj, String propertyName) =>
            ObjectManipulator.GetPropertyValue<T>(obj, propertyName);

        public Boolean TryGetPropertyValue(Object obj, String propertyName, out Object result) =>
            ObjectManipulator.TryGetPropertyValue(obj, propertyName, out result);

        public Boolean TryGetPropertyValue<T>(Object obj, String propertyName, out T result) =>
            ObjectManipulator.TryGetPropertyValue(obj, propertyName, out result);

        public IEnumerable<NamedValueNode> GetPropertyResults(ValueNode value, ISerializationDepth depth)
            => ObjectManipulator.GetPropertyResults(value, depth);

        public Boolean SetFieldValue(Type classType, String fieldName, Object targetObj, Object propVal) =>
            ObjectManipulator.SetFieldValue(classType, fieldName, targetObj, propVal);

        public Boolean SetFieldValue<T>(Type classType, String fieldName, Object targetObj, Object fieldVal)
            => ObjectManipulator.SetFieldValue<T>(classType, fieldName, targetObj, fieldVal);

        public void SetFieldValues<TObject>(TObject obj, Action<ITypeStructure, TObject> action)
            => ObjectManipulator.SetFieldValues(obj, action);

        public Boolean SetProperty(Type classType, String propName, ref Object targetObj,
            Object propVal) => ObjectManipulator.SetProperty(classType, propName,
            ref targetObj, propVal);

        public Boolean SetPropertyValue(ref Object targetObj, String propName, Object propVal)
            => ObjectManipulator.SetPropertyValue(ref targetObj, propName, propVal);

        public void Method(Object obj, String methodName, Object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            ObjectManipulator.Method(obj, methodName, parameters, flags);
        }

        public void GenericMethod(Object obj, String methodName, Type[] genericParameters, Object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            ObjectManipulator.GenericMethod(obj, methodName, genericParameters, parameters, flags);
        }

        public Object Func(Object obj, String funcName, Object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public) =>
            ObjectManipulator.Func(obj, funcName, parameters, flags);

        public Object GenericFunc(Object obj, String funcName, Object[] parameters, Type[] genericParameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public) =>
            ObjectManipulator.GenericFunc(obj, funcName, parameters, genericParameters, flags);

        public T CastDynamic<T>(Object o) => ObjectManipulator.CastDynamic<T>(o);

        public T CastDynamic<T>(Object o, IObjectConverter converter, ISerializerSettings settings)
            => ObjectManipulator.CastDynamic<T>(o, converter, settings);

        public Boolean TryCastDynamic<T>(Object o, out T casted) => ObjectManipulator.TryCastDynamic(o, out casted);

        public PropertySetter CreateSetMethod(MemberInfo memberInfo)
            => TypeManipulator.CreateSetMethod(memberInfo);
    }
}