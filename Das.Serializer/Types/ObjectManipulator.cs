using Das.Serializer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Das.Serializer.Objects;

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
                ConcurrentDictionary<string, VoidMethod>>();
            _cachedFuncs = new ConcurrentDictionary<Type, 
                ConcurrentDictionary<string, Func<object, object[], object>>>();
        }

        public NamedValueNode GetPropertyResult(object o, Type asType, string propertyName)
        {
            if (propertyName == null)
                return default;
            var str = GetStructure(asType, SerializationDepth.AllProperties);
            return str.GetPropertyValue(o, propertyName);
        }

        public T GetPropertyValue<T>(object obj, string propertyName)
            => (T) GetPropertyResult(obj, obj.GetType(),propertyName).Value;

        public bool TryGetPropertyValue(object obj, string propertyName, out object result)
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

        public bool TryGetPropertyValue<T>(object obj, string propertyName, out T result)
        {
            if (TryGetPropertyValue(obj, propertyName, out object res))
            {
                if (TryCastDynamic(res, out result))
                    return true;
            }
            result = default;
            return false;
        }

        public IEnumerable<NamedValueNode> GetPropertyResults(ValueNode value, 
            ISerializationDepth depth)
        {
            var val = value?.Value;

            if (val == null)
                yield break;

            var useType = _typeDelegates.IsUseless(value.Type) ? val.GetType() : value.Type;
            var isReturnNulls = !depth.IsOmitDefaultValues;
            var typeStruct = GetStructure(useType, SerializationDepth.AllProperties);

            foreach (var res in typeStruct.GetPropertyValues(val, depth.SerializationDepth))
            {
                if (isReturnNulls || res.Value != null)
                    yield return res;
            }
        }

        public bool SetFieldValue(Type classType, string fieldName,
            object targetObj, object propVal)
        {
            var str = GetStructure(classType, SerializationDepth.Full);
            return str.SetFieldValue(fieldName, targetObj, propVal);
        }

        public bool SetFieldValue<T>(Type classType, string fieldName, object targetObj,
            object fieldVal)
        {
            var str = GetStructure(classType, SerializationDepth.Full);
            return str.SetFieldValue<T>(fieldName, targetObj, fieldVal);
        }

        public void SetFieldValues<TObject>(TObject obj, Action<ITypeStructure, TObject> action)
        {
            var s = GetStructure(typeof(TObject), SerializationDepth.Full);
            action(s, obj);
        }

        public void SetFieldValues<TObject>(TObject obj, Action<TypeStructure, TObject> action)
        {
            var s = GetStructure(typeof(TObject), SerializationDepth.Full);
            action(s, obj);
        }

        public void SetFieldValues<TObject>(object obj, Action<ITypeStructure> action)
        {
            var str = GetStructure(typeof(TObject), SerializationDepth.Full);
            action(str);

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
        public bool SetProperty(Type classType, string propName, ref object targetObj, object propVal)
        {
            var str = GetStructure(classType, SerializationDepth.AllProperties);
            return str.SetValue(propName, ref targetObj, propVal, SerializationDepth.AllProperties);
        }

        public bool SetPropertyValue(ref object targetObj, string propName, object propVal)
            => SetProperty(targetObj.GetType(), propName, ref targetObj, propVal);

        public void Method(object obj, string methodName, object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            var type = obj as Type ?? obj.GetType();

            if (!_cachedMethods.TryGetValue(type, out var meths))
            {
                meths = _cachedMethods.GetOrAdd(type,
                    new ConcurrentDictionary<string, VoidMethod>());
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

        public void GenericMethod(object obj, string methodName, Type[] genericParameters, object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            var meth = obj.GetType().FindMethod(methodName, parameters, flags);
            meth = meth.MakeGenericMethod(genericParameters);
            meth.Invoke(obj, parameters);
        }

        public object Func(object obj, string funcName, object[] parameters,
            BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public)
        {
            var type = obj as Type ?? obj.GetType();
            if (!_cachedFuncs.TryGetValue(type, out var funcs))
            {
                funcs = _cachedFuncs.GetOrAdd(type,
                    new ConcurrentDictionary<string, Func<Object, Object[], Object>>());
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

        public object GenericFunc(object obj, string funcName, object[] parameters, Type[] genericParameters,
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
                result = (T)Convert.ChangeType(o, tt);
                return true;
            }

            result = default;
            return false;
        }

        public T CastDynamic<T>(object o)
        {
            if (TryCleanCast<T>(o, out var good))
                return good;

            throw new InvalidCastException();
        }

        public T CastDynamic<T>(object o, IObjectConverter converter, ISerializerSettings settings)
        {
            if (TryCleanCast<T>(o, out var good))
                return good;

            return converter.ConvertEx<T>(o, settings);
        }

        public bool TryCastDynamic<T>(object o, out T casted)
        {
            if (TryCleanCast(o, out casted))
                return true;

            casted = default;
            return false;
        }

        private TypeStructure GetStructure(Type type, SerializationDepth depth) 
            => _typeDelegates.GetStructure(type, depth);

    }
}
