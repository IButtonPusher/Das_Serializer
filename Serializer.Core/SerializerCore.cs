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
                ? conc : new ConcurrentDictionary<Type, Type>(Surrogates);
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
       
        public object BuildDefault(Type type, Boolean isCacheConstructors) 
            => ObjectInstantiator.BuildDefault(type, isCacheConstructors);

        public T BuildDefault<T>(Boolean isCacheConstructors) 
            => ObjectInstantiator.BuildDefault<T>(isCacheConstructors);

        public Delegate GetConstructorDelegate(Type type, Type delegateType) => ObjectInstantiator.GetConstructorDelegate(type, delegateType);

        public Func<object> GetConstructorDelegate(Type type) => ObjectInstantiator.GetConstructorDelegate(type);

        public void OnDeserialized(object obj, SerializationDepth depth)
        {
            ObjectInstantiator.OnDeserialized(obj, depth);
        }

        public bool TryGetPropertiesConstructor(Type type, out ConstructorInfo constr) => ObjectInstantiator.TryGetPropertiesConstructor(type, out constr);

        public T CreatePrimitiveObject<T>(byte[] rawValue, Type objType) => ObjectInstantiator.CreatePrimitiveObject<T>(rawValue, objType);

        public object CreatePrimitiveObject(byte[] rawValue, Type objType) => ObjectInstantiator.CreatePrimitiveObject(rawValue, objType);

        public Type GetTypeFromClearName(string clearName) => TypeInferrer.GetTypeFromClearName(clearName);

        public string ToClearName(Type type, bool isOmitAssemblyName) => TypeInferrer.ToClearName(type, isOmitAssemblyName);

        public Type GetGermaneType(Type ownerType) => TypeInferrer.GetGermaneType(ownerType);

        public Type GetGermaneType(Object mustBeCollection) 
            => TypeInferrer.GetGermaneType(mustBeCollection);

        public void ClearCachedNames()
        {
            TypeInferrer.ClearCachedNames();
        }

        public int BytesNeeded(Type typ) => TypeInferrer.BytesNeeded(typ);

        public IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type) 
            => TypeManipulator.GetRecursivePrivateFields(type);

        public bool IsDefaultValue(object o) => TypeInferrer.IsDefaultValue(o);

        public string ToPropertyStyle(string name) => TypeInferrer.ToPropertyStyle(name);

        public VoidMethod GetAdder(IEnumerable collection, Type type = null) => TypeManipulator.GetAdder(collection, type);

        public Func<object, object> CreatePropertyGetter(Type targetType, PropertyInfo propertyInfo) => TypeManipulator.CreatePropertyGetter(targetType, propertyInfo);

        public Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
            out Action<Object, Object> setter)
            => TypeManipulator.TryCreateReadOnlyPropertySetter(propertyInfo, out setter);

        public Func<object, object> CreateFieldGetter(FieldInfo fieldInfo) => TypeManipulator.CreateFieldGetter(fieldInfo);

        public Action<object, object> CreateFieldSetter(FieldInfo fieldInfo) => TypeManipulator.CreateFieldSetter(fieldInfo);

        public VoidMethod CreateMethodCaller(MethodInfo method) => TypeManipulator.CreateMethodCaller(method);

        public Func<object, object[], object> CreateFuncCaller(MethodInfo method) => TypeManipulator.CreateFuncCaller(method);

        public MethodInfo GetAddMethod<T>(IEnumerable<T> collection) => TypeManipulator.GetAddMethod(collection);

        public Type GetPropertyType(Type classType, string propName) => TypeManipulator.GetPropertyType(classType, propName);

        public int PropertyCount(Type type) => TypeManipulator.PropertyCount(type);

        public ITypeStructure GetStructure(Type type, SerializationDepth depth) 
            => TypeManipulator.GetStructure(type, depth);

        public ITypeStructure GetStructure<T>(SerializationDepth depth) 
            => TypeManipulator.GetStructure<T>(depth);

        public IEnumerable<MemberInfo> GetPropertiesToSerialize(Type type,
            SerializationDepth depth) 
            => TypeManipulator.GetPropertiesToSerialize(type, depth);

        public Type InstanceMemberType(MemberInfo info) => TypeManipulator.InstanceMemberType(info);

        public IEnumerable<MethodInfo> GetInterfaceMethods(Type type) => TypeManipulator.GetInterfaceMethods(type);
        public NamedValueNode GetPropertyResult(object o, Type asType, string propertyName) => 
            ObjectManipulator.GetPropertyResult(o, asType, propertyName);

        public T GetPropertyValue<T>(object obj, string propertyName) => ObjectManipulator.GetPropertyValue<T>(obj, propertyName);

        public bool TryGetPropertyValue(object obj, string propertyName, out object result) => ObjectManipulator.TryGetPropertyValue(obj, propertyName, out result);

        public bool TryGetPropertyValue<T>(object obj, string propertyName, out T result) => ObjectManipulator.TryGetPropertyValue(obj, propertyName, out result);

        public IEnumerable<NamedValueNode> GetPropertyResults(ValueNode value,  ISerializationDepth depth)
            => ObjectManipulator.GetPropertyResults(value, depth);

        public bool SetFieldValue(Type classType, string fieldName, object targetObj, object propVal) => ObjectManipulator.SetFieldValue(classType, fieldName, targetObj, propVal);

        public bool SetFieldValue<T>(Type classType, string fieldName, object targetObj, object fieldVal) 
            => ObjectManipulator.SetFieldValue<T>(classType, fieldName, targetObj, fieldVal);

        public void SetFieldValues<TObject>(TObject obj, Action<ITypeStructure, TObject> action)
            => ObjectManipulator.SetFieldValues(obj, action);

        public bool SetProperty(Type classType, string propName, ref object targetObj, 
            object propVal) => ObjectManipulator.SetProperty(classType, propName, 
                ref targetObj, propVal);

        public bool SetPropertyValue(ref object targetObj, string propName, object propVal) 
            => ObjectManipulator.SetPropertyValue(ref targetObj, propName, propVal);

        public void Method(object obj, string methodName, object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            ObjectManipulator.Method(obj, methodName, parameters, flags);
        }

        public void GenericMethod(object obj, string methodName, Type[] genericParameters, object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            ObjectManipulator.GenericMethod(obj, methodName, genericParameters, parameters, flags);
        }

        public object Func(object obj, string funcName, object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public) =>
            ObjectManipulator.Func(obj, funcName, parameters, flags);

        public object GenericFunc(object obj, string funcName, object[] parameters, Type[] genericParameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public) =>
            ObjectManipulator.GenericFunc(obj, funcName, parameters, genericParameters, flags);

        public T CastDynamic<T>(object o) => ObjectManipulator.CastDynamic<T>(o);

        public T CastDynamic<T>(object o, IObjectConverter converter, ISerializerSettings settings) 
            => ObjectManipulator.CastDynamic<T>(o, converter, settings);

        public bool TryCastDynamic<T>(object o, out T casted) => ObjectManipulator.TryCastDynamic(o, out casted);

        public PropertySetter CreateSetMethod(MemberInfo memberInfo)
            => TypeManipulator.CreateSetMethod(memberInfo);       
    }
}
