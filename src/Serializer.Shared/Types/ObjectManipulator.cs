﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Types
{
    public class ObjectManipulator : IObjectManipulator
    {
        public ObjectManipulator(ITypeManipulator typeDelegates,
                                 ISerializerSettings settings)
        {
            _typeDelegates = typeDelegates;
            _settings = settings;
            _cachedMethods = new ConcurrentDictionary<Type,
                ConcurrentDictionary<String, VoidMethod>>();
            _cachedFuncs = new ConcurrentDictionary<Type,
                ConcurrentDictionary<String, Func<Object, Object[], Object>>>();
        }

        //public IProperty? GetPropertyResult(Object o,
        //                                    Type asType,
        //                                    String propertyName)
        //{
        //    if (propertyName == null)
        //        return default;
        //    var str = _typeDelegates.GetTypeStructure(asType, DepthConstants.AllProperties);
        //    return str.GetProperty(o, propertyName);
        //}

        //public IEnumerable<IProperty> GetPropertyValues<T>(T obj)
        //{
        //    var node = new ValueNode(obj!, typeof(T));
        //    return GetPropertyResults(node, _settings);
        //}


        public T GetPropertyValue<T>(Object obj,
                                     String propertyName)
        {
            var res = GetPropertyValue(obj, propertyName, PropertyNameFormat.Default);
            switch (res)
            {
                case T good:
                    return good;

                default:
                    throw new InvalidCastException();
            }

            //return (T) GetPropertyResult(obj, obj.GetType(), propertyName)?.Value!;
        }

        public TProperty GetPropertyValue<TObject, TProperty>(TObject obj,
                                                              String propertyName)
        {
            var str = _typeDelegates.GetTypeStructure(typeof(TObject));

            return str.GetPropertyValue<TObject, TProperty>(obj, propertyName);
        }

        public Object? GetPropertyValue(Object obj,
                                        String propertyName,
                                        PropertyNameFormat format)
        {
            var str = _typeDelegates.GetTypeStructure(obj.GetType());
            return str.GetPropertyValue(obj, propertyName, format);
        }

        public Boolean TryGetPropertyValue(Object obj,
                                           String propertyName,
                                           out Object result)
        {
            if (obj == null)
            {
                result = null!;
                return false;
            }

            var oType = obj is Type t ? t : obj.GetType();

            var str = _typeDelegates.GetTypeStructure(oType);//, DepthConstants.AllProperties);
            result = str.GetPropertyValue(obj, propertyName)!;

            //var propRes = GetPropertyResult(obj, oType, propertyName);

            //result = propRes?.Value!;
            return result != null;
        }

        public Boolean TryGetPropertyValue<T>(Object obj,
                                              String propertyName,
                                              out T result)
        {
            if (TryGetPropertyValue(obj, propertyName, out var res))
                if (TryCastDynamic(res, out result))
                    return true;

            result = default!;
            return false;
        }

        //public IPropertyValueIterator<IProperty> GetPropertyResults(IValueNode value,
        //                                                            ISerializationDepth depth)
        //{
        //    var val = value.Value;

        //    if (val == null)
        //        return new PropertyValueIterator<IProperty>();

        //    var useType = _typeDelegates.IsUseless(value.Type)
        //        ? val.GetType()
        //        : value.Type;

        //    var typeStruct = _typeDelegates.GetTypeStructure(useType!, DepthConstants.AllProperties);
        //    var found = typeStruct.GetPropertyValues(val, depth);
        //    return found;
        //}

        public IEnumerable<KeyValuePair<PropertyInfo, object?>> GetPropertyResults(Object? val)
        {
            if (val == null)
                yield break;

            foreach (var kvp in GetPropertyResults(val, val.GetType(), _settings))
            {
                yield return kvp;
            }
        }

        public IEnumerable<KeyValuePair<PropertyInfo, Object?>> GetPropertyResults(Object? val,
            Type valType,
            ISerializationDepth depth)
        {
            if (val == null)
                yield break;

            var useType = _typeDelegates.IsUseless(valType)
                ? val.GetType()
                : valType;

            var typeStruct = _typeDelegates.GetTypeStructure(useType);//!, DepthConstants.AllProperties);
            foreach (var f in typeStruct.IteratePropertyValues(val, depth))
            {
                yield return f;
            }
        }

        public Boolean SetFieldValue(Type classType,
                                     String fieldName,
                                     Object targetObj,
                                     Object propVal)
        {
            var str = _typeDelegates.GetTypeStructure(classType);//, DepthConstants.Full);
            return str.SetFieldValue(fieldName, targetObj, propVal);
        }

        public Boolean SetFieldValue<T>(Type classType,
                                        String fieldName,
                                        Object targetObj,
                                        Object fieldVal)
        {
            var str = _typeDelegates.GetTypeStructure(classType);//, DepthConstants.Full);
            return str.SetFieldValue<T>(fieldName, targetObj, fieldVal);
        }

        public void SetFieldValues<TObject>(TObject obj,
                                            Action<ITypeStructure, TObject> action)
        {
            var s = _typeDelegates.GetTypeStructure(typeof(TObject));//, DepthConstants.Full);
            action(s, obj);
        }

        /// <summary>
        ///     Attempts to set a property value for a targetObj which is a property of name propName
        ///     in a class of type classType
        /// </summary>
        public Boolean TrySetProperty(Type classType,
                                      String propName,
                                      PropertyNameFormat format,
                                      ref Object targetObj,
                                      Object? propVal)
        {
            var str = _typeDelegates.GetTypeStructure(classType);//, DepthConstants.AllProperties);
            return str.TrySetPropertyValue(propName, format, ref targetObj, propVal);
        }

        public void SetMutableProperties(IEnumerable<PropertyInfo> mutable,
                                         Object source,
                                         Object target)
        {
            foreach (var m in mutable)
            {
                if (!TryGetPropertyValue(source, m.Name, out var propVal))
                    continue;

                TrySetProperty(target.GetType(), m.Name, PropertyNameFormat.Default, ref target, propVal);
            }
        }

        public Boolean SetPropertyValue(ref Object targetObj,
                                        String propName,
                                        PropertyNameFormat format,
                                        Object? propVal)
        {
            return TrySetProperty(targetObj.GetType(), propName, format, ref targetObj!, propVal);
        }

        public void Method(Object obj,
                           String methodName,
                           Object[] parameters,
                           BindingFlags flags = BindingFlags.Default | BindingFlags.Instance |
                                                BindingFlags.Public)
        {
            var type = obj as Type ?? obj.GetType();

            if (!_cachedMethods.TryGetValue(type, out var meths))
                meths = _cachedMethods.GetOrAdd(type,
                    new ConcurrentDictionary<String, VoidMethod>());

            if (!meths.TryGetValue(methodName, out var target))
            {
                var meth = type.FindMethod(methodName, parameters, flags) ??
                           throw new MissingMethodException(type.FullName, methodName);

                if (meth.IsGenericMethod)
                    throw new NotSupportedException(
                        "Use the GenericMethod(...) extension for this method");

                target = _typeDelegates.CreateMethodCaller(meth);

                meths.TryAdd(methodName, target);
            }

            target(obj, parameters);
        }

        public void GenericMethod(Object obj,
                                  String methodName,
                                  Type[] genericParameters,
                                  Object[] parameters,
                                  BindingFlags flags =
                                      BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            var meth = obj.GetType().FindMethod(methodName, parameters, flags) ??
                       throw new MissingMethodException(obj.GetType().FullName, methodName);
            meth = meth.MakeGenericMethod(genericParameters);
            meth.Invoke(obj, parameters);
        }

        public Object Func(Object obj,
                           String funcName,
                           Object[] parameters,
                           BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            var type = obj as Type ?? obj.GetType();
            if (!_cachedFuncs.TryGetValue(type, out var funcs))
                funcs = _cachedFuncs.GetOrAdd(type,
                    new ConcurrentDictionary<String, Func<Object, Object[], Object>>());

            if (funcs.TryGetValue(funcName, out var target))
                return target(obj, parameters);

            var meth = type.FindMethod(funcName, parameters, flags) ??
                       throw new MissingMethodException(type.FullName, funcName);

            #region TODO: don't use reflection for generic methods

            if (meth.IsGenericMethod) throw new NotSupportedException("Use GenericFunc() for this function");

            #endregion

            target = _typeDelegates.CreateFuncCaller(meth);
            funcs.TryAdd(funcName, target);

            return target(obj, parameters);
        }

        public Object GenericFunc(Object obj,
                                  String funcName,
                                  Object[] parameters,
                                  Type[] genericParameters,
                                  BindingFlags flags =
                                      BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)

        {
            var meth = obj.GetType().FindMethod(funcName, parameters, flags) ??
                       throw new MissingMethodException(obj.GetType().FullName, funcName);
            meth = meth.MakeGenericMethod(genericParameters);

            return meth.Invoke(obj, parameters)!;
        }

        public T CastDynamic<T>(Object o)
        {
            if (TryCleanCast<T>(o, out var good))
                return good;

            throw new InvalidCastException();
        }

        public T CastDynamic<T>(Object o,
                                IObjectConverter converter,
                                ISerializerSettings settings)
        {
            if (TryCleanCast<T>(o, out var good))
                return good;

            return converter.ConvertEx<T>(o, settings);
        }

        public Boolean TryCastDynamic<T>(Object o,
                                         out T casted)
        {
            if (TryCleanCast(o, out casted))
                return true;

            casted = default!;
            return false;
        }

        private static Boolean TryCleanCast<T>(Object o,
                                               out T result)
        {
            if (o is T ez)
            {
                result = ez;
                return true;
            }

            var tt = typeof(T);

            if (typeof(IConvertible).IsAssignableFrom(tt) &&
                o is IConvertible)
            {
                result = (T) Convert.ChangeType(o, tt);
                return true;
            }

            var implicitOperators = from m in o.GetType().GetMethods()
                let mparams = m.GetParameters()
                where string.Equals(m.Name, "op_Implicit") &&
                      m.ReturnType == tt &&
                      mparams.Length == 1 &&
                      mparams[0].ParameterType == o.GetType()
                select m;

            var useImplicit = implicitOperators.FirstOrDefault();
            if (useImplicit != null)
            {
                result = (T) useImplicit.Invoke(null, new[] {o});
                return true;
            }

            result = default!;
            return false;
        }

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary
            <String, Func<Object, Object[], Object>>> _cachedFuncs;

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary
            <String, VoidMethod>> _cachedMethods;

        private readonly ISerializerSettings _settings;
        private readonly ITypeManipulator _typeDelegates;
    }
}
