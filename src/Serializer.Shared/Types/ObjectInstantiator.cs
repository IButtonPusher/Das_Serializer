#if !GENERATECODE
using System.Linq.Expressions;
#else
using System.Reflection.Emit;
#endif
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class ObjectInstantiator : TypeCore,
                                      IInstantiator
    {
        static ObjectInstantiator()
        {
            InstantionTypes = new ConcurrentDictionary<Type, InstantiationType>();
            ConstructorDelegates = new ConcurrentDictionary<Type, Func<Object>>();
            Constructors = new ConcurrentDictionary<Type, ConstructorInfo?>();
            KnownOnDeserialize = new ConcurrentDictionary<Type, Boolean>();
            GenericListDelegates = new ConcurrentDictionary<Type, Func<IList>>();
        }


        public ObjectInstantiator(ITypeInferrer typeInferrer,
                                  ITypeManipulator typeManipulator,
                                  IDictionary<Type, Type> typeSurrogates,
                                  IObjectManipulator objectManipulator,
                                  IDynamicTypes dynamicTypes)
            : base(typeManipulator.Settings)
        {
            _typeInferrer = typeInferrer;
            _typeManipulator = typeManipulator;
            _typeSurrogates = new Dictionary<Type, Type>(typeSurrogates);
            _objectManipulator = objectManipulator;
            _dynamicTypes = dynamicTypes;
        }

        public Object? BuildDefault(Type type,
                                    Boolean isCacheConstructors)
        {
            if (_typeSurrogates.ContainsKey(type))
                type = _typeSurrogates[type];

            var instType = GetInstantiationType(type);

            switch (instType)
            {
                case InstantiationType.EmptyString:
                    return String.Empty;

                case InstantiationType.DefaultConstructor:
                    if (isCacheConstructors)
                        return CreateInstanceCacheConstructor(type);
                    else
                        return CreateInstanceDetectConstructor(type);

                //return Activator.CreateInstance(type);

                case InstantiationType.Emit:
                    if (isCacheConstructors)
                        return CreateInstanceCacheConstructor(type);
                    else
                        return CreateInstanceDetectConstructor(type);

                case InstantiationType.EmptyArray:
                    var germane = _typeInferrer.GetGermaneType(type);
                    return Array.CreateInstance(germane, 0);
                case InstantiationType.Uninitialized:
                    return FormatterServices.GetUninitializedObject(type);
                case InstantiationType.NullObject:
                    return null;
                case InstantiationType.Abstract:
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
            return def == null ? default! : (T) def;
        }

        public IList BuildGenericList(Type type)
        {
            var f = GenericListDelegates.GetOrAdd(type,
                t => GetConstructorDelegate<Func<IList>>(typeof(List<>).MakeGenericType(t)));


            return f();
        }

        public TDelegate GetConstructorDelegate<TDelegate>(Type type)
            where TDelegate : Delegate
        {
            if (TryGetConstructorDelegate<TDelegate>(type, out var res))
                return res;

            throw new InvalidProgramException(
                $"Type '{type.Name}' doesn't have the requested constructor.");
        }

        public bool TryGetConstructorDelegate<TDelegate>(Type type,
                                                         out TDelegate result)
            where TDelegate : Delegate
        {
            if (TryGetConstructorDelegate(type, typeof(TDelegate), out var maybe)
                && maybe is TDelegate td)
            {
                result = td;
                return true;
            }

            result = default!;
            return false;
        }

        public void OnDeserialized(IValueNode node,
                                   ISerializationDepth depth)
        {
            if (node.Type == null || node.Value == null)
                return;

            var wasKnown = KnownOnDeserialize.TryGetValue(node.Type, out var dothProceed);

            if (wasKnown && !dothProceed)
                return;

            var str = _typeManipulator.GetTypeStructure(node.Type, depth);
            dothProceed = str.OnDeserialized(node.Value, _objectManipulator);
            if (!wasKnown)
                KnownOnDeserialize.TryAdd(node.Type, dothProceed);
        }


        public T CreatePrimitiveObject<T>(Byte[] rawValue,
                                          Type objType)
        {
            if (rawValue.Length == 0)
                return default!;

            var handle = GCHandle.Alloc(rawValue, GCHandleType.Pinned);
            var structure = (T) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), objType)!;
            handle.Free();
            return structure!;
        }

        public Object CreatePrimitiveObject(Byte[] rawValue,
                                            Type objType)
        {
            return CreatePrimitiveObject<Object>(rawValue, objType);
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

        public bool TryGetDefaultConstructor(Type type,
                                             out ConstructorInfo ctor)
        {
            if (Constructors.TryGetValue(type, out ctor!))
            {
                if (ctor == null)
                    return false;

                if (ctor.GetParameters().Length == 0)
                    return true;

                ctor = GetConstructor(type, new List<Type>(), out _)!;
                goto byeNow;
            }

            ctor = GetConstructor(type, new List<Type>(), out _)!;
            Constructors.TryAdd(type, ctor);

            byeNow:
            return ctor != null;
        }

        public bool TryGetDefaultConstructor<T>(out ConstructorInfo? ctor)
        {
            var type = typeof(T);

            return TryGetDefaultConstructor(type, out ctor);
        }

        public bool TryGetDefaultConstructorDelegate<T>(out Func<T> res) where T : class
        {
            var type = typeof(T);
            if (ConstructorDelegates.TryGetValue(type, out var constructor)
                && constructor is Func<T> good)
            {
                res = good;
                return true;
            }

            if (!TryGetConstructorDelegate<Func<T>>(typeof(T), out var del))
            {
                res = default!;
                return false;
            }

            constructor = del;
            ConstructorDelegates.TryAdd(type, constructor);

            res = (constructor as Func<T>)!;
            return true;
        }

        public Func<T> GetDefaultConstructor<T>() where T : class
        {
            var type = typeof(T);
            if (!ConstructorDelegates.TryGetValue(type, out var constructor))
            {
                constructor = GetConstructorDelegate<T>();
                ConstructorDelegates.TryAdd(type, constructor);
            }

            return (constructor as Func<T>)!;
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
            var ctor = GetConstructor(type, new List<Type>(), out _)
                       ?? throw new MissingMethodException(type.Name);
            var ctored = ctor.Invoke(new Object[0]);
            return ctored;
        }

        private static ConstructorInfo? GetConstructor(Type type,
                                                       ICollection<Type> genericArguments,
                                                       out Type[] argTypes)
        {
            argTypes = genericArguments.Count > 1
                ? genericArguments.Take(genericArguments.Count - 1).ToArray()
                : Type.EmptyTypes;

            return type.GetConstructor(Const.AnyInstance, null, argTypes, null);
        }

        public Func<T> GetConstructorDelegate<T>()
        {
            return GetConstructorDelegate<Func<T>>(typeof(T));
        }


        private static Func<Object> GetConstructorDelegate(Type type)
        {
            var delType = typeof(Func<>).MakeGenericType(type);

            if (TryGetConstructorDelegate(type, delType, out var res))
                return (Func<Object>) res;

            throw new InvalidProgramException(
                $"Type '{type.Name}' doesn't have the requested constructor.");
        }

        private InstantiationType GetInstantiationType(Type type)
        {
            if (InstantionTypes.TryGetValue(type, out var res))
                return res;
            if (type == typeof(String))
                res = InstantiationType.EmptyString;
            else if (type.IsArray)
                res = InstantiationType.EmptyArray;
            else if (!type.IsAbstract)
            {
                if (_typeInferrer.HasEmptyConstructor(type))
                    res = _typeInferrer.IsCollection(type)
                        ? InstantiationType.DefaultConstructor
                        : InstantiationType.Emit;
                else if (type.IsGenericType)
                    res = InstantiationType.NullObject; //Nullable<T>
                else
                    res = InstantiationType.Uninitialized;
            }
            else if (_typeInferrer.IsCollection(type))
                res = InstantiationType.EmptyArray;
            else
                return InstantiationType.Abstract;

            InstantionTypes.TryAdd(type, res);
            return res;
        }

        private static bool TryGetConstructorDelegate(Type type,
                                                      Type delegateType,
                                                      out Delegate result)
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

            #if GENERATECODE

            var ownerType = type.IsGenericType
                ? typeof(ObjectInstantiator)
                : type;
            //if (type.IsGenericType)
            //{
            //    var garg =  type.GetGenericArguments()[0];
            //    if (!garg.IsValueType && garg != typeof(String))
            //    {
            //        if (!garg.IsInterface)
            //            ownerType = garg;
            //        else 
            //            ownerType = typeof(ObjectInstantiator);
            //    }
            //}

            var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, argTypes, ownerType);
            //var dynamicMethod = new DynamicMethod("DM$_" + type.Name, type, argTypes, type);

            var ilGen = dynamicMethod.GetILGenerator();

            for (var i = 0; i < argTypes.Length; i++)
                ilGen.Emit(OpCodes.Ldarg, i);

            ilGen.Emit(OpCodes.Newobj, constructor);


            ilGen.Emit(OpCodes.Ret);
            result = dynamicMethod.CreateDelegate(delegateType);
            return true;

            #else
            if (argTypes.Length == 0)
            {
                result = CreateConstructor(constructor);
                return true;
            }
            else
            {
                result = CreateConstructor(constructor, argTypes);
                return true;
            }

            #endif
        }

        #if !GENERATECODE
        private static Func<Object[], Object> CreateConstructor(ConstructorInfo cInfo, 
                                                                Type[] paramArguments)
        {
            // compile the call

            var parameterExpression = Expression.Parameter(typeof(object[]), "arguments");

            List<Expression> argumentsExpressions = new List<Expression>();
            for (var i = 0; i < paramArguments.Length; i++)
            {

                var indexedAcccess = Expression.ArrayIndex(parameterExpression, Expression.Constant(i));

                // it is NOT a reference type!
                if (paramArguments[i].IsClass == false && paramArguments[i].IsInterface == false)
                {
                    // it might be the case when I receive null and must convert to a structure. In  this case I must put default (ThatStructure).
                    var localVariable = Expression.Variable(paramArguments[i], "localVariable");

                    var block = Expression.Block(new[] {localVariable},
                        Expression.IfThenElse(Expression.Equal(indexedAcccess, Expression.Constant(null)),
                            Expression.Assign(localVariable, Expression.Default(paramArguments[i])),
                            Expression.Assign(localVariable, Expression.Convert(indexedAcccess, paramArguments[i]))
                        ),
                        localVariable
                    );

                    argumentsExpressions.Add(block);

                }
                else
                    argumentsExpressions.Add(Expression.Convert(indexedAcccess,
                        paramArguments[i])); // do a convert to that reference type. If null, the convert is FINE.
            }

            // check if parameters length maches the length of constructor parameters!
            var lengthProperty = typeof(Object[]).GetProperty("Length") ?? throw new InvalidOperationException();
            var len = Expression.Property(parameterExpression, lengthProperty);

            var checkLengthExpression = Expression.IfThen(
                Expression.NotEqual(len, Expression.Constant(paramArguments.Length)),
                Expression.Throw(Expression.New(typeof(ArgumentException).GetConstructor(new[] {typeof(string)})!,
                    Expression.Constant("The length does not match parameters number")))
            );

            var newExpr = Expression.New(cInfo, argumentsExpressions);

            var finalBlock = Expression.Block(checkLengthExpression, Expression.Convert(newExpr, typeof(Object)));

            var ctor = (Func<Object[], Object>)
                
                Expression.Lambda(finalBlock, new[] {parameterExpression}).Compile();

            return ctor;
        }

        private static Func<Object> CreateConstructor(ConstructorInfo cInfo)
        {
            var newExpr = Expression.New(cInfo);

            var finalBlock = Expression.Block(Expression.Convert(newExpr, typeof(Object)));

            var ctor = (Func<Object>) Expression.Lambda(finalBlock).Compile();

            return ctor;
        }

        #endif


        private static readonly ConcurrentDictionary<Type, InstantiationType> InstantionTypes;
        private static readonly ConcurrentDictionary<Type, Func<Object>> ConstructorDelegates;
        private static readonly ConcurrentDictionary<Type, Func<IList>> GenericListDelegates;
        private static readonly ConcurrentDictionary<Type, ConstructorInfo?> Constructors;

        private static readonly ConcurrentDictionary<Type, Boolean> KnownOnDeserialize;
        private readonly IDynamicTypes _dynamicTypes;
        private readonly IObjectManipulator _objectManipulator;
        private readonly ITypeInferrer _typeInferrer;
        private readonly ITypeManipulator _typeManipulator;
        private readonly Dictionary<Type, Type> _typeSurrogates;
    }
}
