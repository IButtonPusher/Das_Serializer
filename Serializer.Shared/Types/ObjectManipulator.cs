using Das.Serializer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Das.Serializer.Objects;
using Interfaces.Shared.Settings;

namespace Das.Types
{
    public class ObjectManipulator : IObjectManipulator
    {
        private readonly TypeManipulator _typeDelegates;

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary
            <String, VoidMethod>> _cachedMethods;

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary
            <String, Func<Object, Object[], Object>>> _cachedFuncs;

        public ObjectManipulator(TypeManipulator typeDelegates)
        {
            _typeDelegates = typeDelegates;
            _cachedMethods = new ConcurrentDictionary<Type,
                ConcurrentDictionary<String, VoidMethod>>();
            _cachedFuncs = new ConcurrentDictionary<Type,
                ConcurrentDictionary<String, Func<Object, Object[], Object>>>();
        }

        public NamedValueNode GetPropertyResult(Object o, Type asType, String propertyName)
        {
            if (propertyName == null)
                return default;
            var str = GetStructure(asType, DepthConstants.AllProperties);
            return str.GetPropertyValue(o, propertyName);
        }

        public T GetPropertyValue<T>(Object obj, String propertyName)
            => (T) GetPropertyResult(obj, obj.GetType(), propertyName).Value;

        public Boolean TryGetPropertyValue(Object obj, String propertyName, out Object result)
        {
            if (obj == null)
            {
                result = null;
                return false;
            }

            var oType = obj is Type t ? t : obj.GetType();

            var propRes = GetPropertyResult(obj, oType, propertyName);

            result = propRes?.Value;
            return true;
        }

        public Boolean TryGetPropertyValue<T>(Object obj, String propertyName, out T result)
        {
            if (TryGetPropertyValue(obj, propertyName, out var res))
            {
                if (TryCastDynamic(res, out result))
                    return true;
            }

            result = default;
            return false;
        }

        public IEnumerable<PropertyValueNode> GetPropertyResults(ValueNode value,
            ISerializationDepth depth)
        {
            var val = value?.Value;

            if (val == null)
                yield break;

            var useType = _typeDelegates.IsUseless(value.Type) ? val.GetType() : value.Type;
            var isReturnNulls = !depth.IsOmitDefaultValues;
            var typeStruct = GetStructure(useType, DepthConstants.AllProperties);

            foreach (var res in typeStruct.GetPropertyValues(val, depth))
            {
                if (isReturnNulls || res.Value != null)
                    yield return res;
            }
        }

        public Boolean SetFieldValue(Type classType, String fieldName,
            Object targetObj, Object propVal)
        {
            var str = GetStructure(classType, DepthConstants.Full);
            return str.SetFieldValue(fieldName, targetObj, propVal);
        }

        public Boolean SetFieldValue<T>(Type classType, String fieldName, Object targetObj,
            Object fieldVal)
        {
            var str = GetStructure(classType, DepthConstants.Full);
            return str.SetFieldValue<T>(fieldName, targetObj, fieldVal);
        }

        public void SetFieldValues<TObject>(TObject obj, Action<ITypeStructure, TObject> action)
        {
            var s = GetStructure(typeof(TObject), DepthConstants.Full);
            action(s, obj);
        }

        /// <summary>
        /// Attempts to set a property value for a targetObj which is a property of name propName
        /// in a class of type classType
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="propName"></param>
        /// <param name="targetObj"></param>
        /// <param name="propVal"></param>
        /// <returns></returns>
        public Boolean SetProperty(Type classType, String propName, ref Object targetObj, Object propVal)
        {
            var str = GetStructure(classType, DepthConstants.AllProperties);
            return str.SetValue(propName, ref targetObj, propVal, SerializationDepth.AllProperties);
        }

        public Boolean SetPropertyValue(ref Object targetObj, String propName, Object propVal)
            => SetProperty(targetObj.GetType(), propName, ref targetObj, propVal);

        public void Method(Object obj, String methodName, Object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | 
                                 BindingFlags.Public)
        {
            var type = obj as Type ?? obj.GetType();

            if (!_cachedMethods.TryGetValue(type, out var meths))
            {
                meths = _cachedMethods.GetOrAdd(type,
                    new ConcurrentDictionary<String, VoidMethod>());
            }

            if (!meths.TryGetValue(methodName, out var target))
            {
                var meth = type.FindMethod(methodName, parameters, flags);

                if (meth.IsGenericMethod)
                    throw new NotSupportedException(
                        "Use the GenericMethod(...) extension for this method");

                target = _typeDelegates.CreateMethodCaller(meth);

                meths.TryAdd(methodName, target);
            }

            target(obj, parameters);
        }

        public void GenericMethod(Object obj, String methodName, Type[] genericParameters, Object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            var meth = obj.GetType().FindMethod(methodName, parameters, flags);
            meth = meth.MakeGenericMethod(genericParameters);
            meth.Invoke(obj, parameters);
        }

        public Object Func(Object obj, String funcName, Object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            var type = obj as Type ?? obj.GetType();
            if (!_cachedFuncs.TryGetValue(type, out var funcs))
            {
                funcs = _cachedFuncs.GetOrAdd(type,
                    new ConcurrentDictionary<String, Func<Object, Object[], Object>>());
            }

            if (funcs.TryGetValue(funcName, out var target))
                return target(obj, parameters);

            var meth = type.FindMethod(funcName, parameters, flags);

            #region TODO: don't use reflection for generic methods

            if (meth.IsGenericMethod)
            {
                throw new NotSupportedException("Use GenericFunc() for this function");
            }

            #endregion

            target = _typeDelegates.CreateFuncCaller(meth);
            funcs.TryAdd(funcName, target);

            return target(obj, parameters);
        }

        public Object GenericFunc(Object obj, String funcName, Object[] parameters, Type[] genericParameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)

        {
            var meth = obj.GetType().FindMethod(funcName, parameters, flags);
            meth = meth.MakeGenericMethod(genericParameters);
            return meth.Invoke(obj, parameters);
        }

        private static Boolean TryCleanCast<T>(Object o, out T result)
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

            result = default;
            return false;
        }

        public T CastDynamic<T>(Object o)
        {
            if (TryCleanCast<T>(o, out var good))
                return good;

            throw new InvalidCastException();
        }

        public T CastDynamic<T>(Object o, IObjectConverter converter, ISerializerSettings settings)
        {
            if (TryCleanCast<T>(o, out var good))
                return good;

            return converter.ConvertEx<T>(o, settings);
        }

        public Boolean TryCastDynamic<T>(Object o, out T casted)
        {
            if (TryCleanCast(o, out casted))
                return true;

            casted = default;
            return false;
        }

        private ITypeStructure GetStructure(Type type, ISerializationDepth depth)
            => _typeDelegates.GetTypeStructure(type, depth);
    }
}