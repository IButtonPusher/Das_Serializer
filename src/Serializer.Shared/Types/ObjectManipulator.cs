using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer;
using Das.Serializer.Types;

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
        }

        public TProperty GetPropertyValue<TObject, TProperty>(TObject obj,
                                                              String propertyName)
        {
           return _typeDelegates.GetPropertyAccessor<TObject, TProperty>(propertyName).
                                 GetPropertyValue(ref obj);
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
           var accessor = PropertyDictionary.GetPropertyAccessor(obj, propertyName);
           if (!accessor.CanRead)
           {
              result = default!;
              return false;
           }

           result = accessor.GetPropertyValue(obj)!;
           return true;
        }

        public bool TryGetPropertyValue(Object obj,
                                        PropertyInfo property,
                                        out Object result)
        {
           var accessor = PropertyDictionary.GetPropertyAccessor(property, _typeDelegates);
           if (!accessor.CanRead)
           {
              result = default!;
              return false;
           }

           result = accessor.GetPropertyValue(obj)!;
           return true;

        }

        
        public Boolean TryGetPropertyValue<T>(Object obj,
                                              String propertyName,
                                              out T result)
        {
           if (TryGetPropertyValue(obj, propertyName, out var res))
           {
              if (res == default)
              {
                 result = default!;
                 return true;
              }
              if (TryCastDynamic(res, out result))
                 return true;
           }

           result = default!;
            return false;
        }

       

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

            var typeStruct = _typeDelegates.GetTypeStructure(useType);
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
            var str = _typeDelegates.GetTypeStructure(classType);
            return str.SetFieldValue(fieldName, targetObj, propVal);
        }

        public Boolean SetFieldValue<T>(Type classType,
                                        String fieldName,
                                        Object targetObj,
                                        Object fieldVal)
        {
            var str = _typeDelegates.GetTypeStructure(classType);
            return str.SetFieldValue<T>(fieldName, targetObj, fieldVal);
        }

        public void SetFieldValues<TObject>(TObject obj,
                                            Action<ITypeStructure, TObject> action)
        {
            var s = _typeDelegates.GetTypeStructure(typeof(TObject));
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
            var str = _typeDelegates.GetTypeStructure(classType);
            return str.TrySetPropertyValue(propName, format, ref targetObj, propVal);
        }

        public void SetMutableProperties(IEnumerable<PropertyInfo> mutable,
                                         Object source,
                                         Object target)
        {
           var srcType = source.GetType();

            foreach (var m in mutable)
            {

               var srcProp = srcType.GetProperty(m.Name);
               if (srcProp == null)
                  continue;

               var accessor = PropertyDictionary.GetPropertyAccessor(srcProp, _typeDelegates);
               if (!accessor.CanRead)
                  continue;

               var srcVal = accessor.GetPropertyValue(source);

               TrySetProperty(target.GetType(), m.Name, PropertyNameFormat.Default, 
                   ref target, srcVal);
            }
        }

        public Boolean SetPropertyValue(ref Object targetObj,
                                        String propName,
                                        PropertyNameFormat format,
                                        Object? propVal)
        {
            return TrySetProperty(targetObj.GetType(), propName, format, ref targetObj, propVal);
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

        public Boolean TryCastDynamic(Object o,
                                      Type castTo,
                                      out Object casted)
        {
            return TryCleanCast(o, castTo, out casted);
        }


        public Boolean TryCastDynamic<T>(Object o,
                                         out T casted)
        {
            if (TryCleanCast(o, out casted))
                return true;

            casted = default!;
            return false;
        }

        private static Boolean TryCleanCast<TOutput>(Object o,
                                               out TOutput result)
        {
            if (o is TOutput ez)
            {
                result = ez;
                return true;
            }

            if (TryCleanCast(o,  typeof(TOutput), out var ores))
            {
                result = (TOutput)ores;
                return true;
            }

            result = default!;
            return false;
        }

        private static Boolean TryCleanCast(Object o,
                                            Type tt,
                                            out Object result)
        {
            if (typeof(IConvertible).IsAssignableFrom(tt) &&
                o is IConvertible)
            {
                result = Convert.ChangeType(o, tt);
                return true;
            }

            var implicitOperators = from m in o.GetType().GetMethods(BindingFlags.Static | BindingFlags.Public)
                let mparams = m.GetParameters()
                where string.Equals(m.Name, "op_Implicit") &&
                      m.ReturnType == tt &&
                      mparams.Length == 1 &&
                      mparams[0].ParameterType == o.GetType()
                select m;

            var useImplicit = implicitOperators.FirstOrDefault();
            if (useImplicit != null)
            {
                result = useImplicit.Invoke(null, new[] { o });
                return true;
            }

            result = default!;
            return false;
        }

        //private static Func<Object, Object>? GetImplicitDelegate(Type convertTo,
        //                                                         Type convertFrom)
        //{
        //    if (GetImplicitOp(convertTo, convertFrom, out var foundIt))
        //    {
        //        return TypeManipulator.CreateMethodCaller<Func<Object, Object>>(foundIt);
        //    }
        //}

        //private static Boolean GetImplicitOp(Type convertTo,
        //                                         Type convertFrom,
        //                                         out MethodInfo useImplicit)
        //{
        //    var implicitOperators = from m in convertFrom.GetMethods(BindingFlags.Static 
        //                                                             | BindingFlags.Public)
        //        let mparams = m.GetParameters()
        //        where string.Equals(m.Name, "op_Implicit") &&
        //              m.ReturnType == convertTo &&
        //              mparams.Length == 1 &&
        //              mparams[0].ParameterType == convertFrom
        //        select m;

        //    useImplicit = implicitOperators.FirstOrDefault()!;
        //    return useImplicit != null;
        //}

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary
            <String, Func<Object, Object[], Object>>> _cachedFuncs;

        //private static readonly ConcurrentDictionary<Type,
        //    ConcurrentDictionary<Type, MethodInfo?>> _cachedImplicitOps
        //    = new ();

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary
            <String, VoidMethod>> _cachedMethods;

        private readonly ISerializerSettings _settings;
        private readonly ITypeManipulator _typeDelegates;
    }
}
