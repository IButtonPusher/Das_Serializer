using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Das.Serializer;
using Serializer;
using Serializer.Core;

namespace Das.Types
{
    public class TypeManipulator : TypeCore, ITypeManipulator
    {
        public TypeManipulator(ISerializerSettings settings) : base(settings)
        {
            CachedAdders = new ConcurrentDictionary<Type, VoidMethod>();

            _knownSensitive = new ConcurrentDictionary<Type, TypeStructure>();
            _knownInsensitive = new ConcurrentDictionary<Type, TypeStructure>();
        }

        private const BindingFlags InterfaceMethodBindings = BindingFlags.Instance |
                                                             BindingFlags.Public | BindingFlags.NonPublic;

        private readonly ConcurrentDictionary<Type, VoidMethod> CachedAdders;


        private readonly ConcurrentDictionary<Type, TypeStructure> _knownSensitive;
        private readonly ConcurrentDictionary<Type, TypeStructure> _knownInsensitive;

        /// <summary>
        /// Returns a delegate that can be invoked to quickly get the value for an object
        /// of targetType
        /// </summary>        
        public Func<Object, Object> CreatePropertyGetter(Type targetType,
            PropertyInfo propertyInfo)
        {
            var setParamType = typeof(object);
            Type[] setParamTypes = { setParamType };
            var setReturnType = typeof(object);

            var owner = typeof(DasType);

            var getMethod = new DynamicMethod(String.Empty, setReturnType,
                    setParamTypes, owner, true);

            var ilCommunication = getMethod.GetILGenerator();

            ilCommunication.Emit(OpCodes.Ldarg_0);

            ilCommunication.Emit(targetType.IsValueType
                ? OpCodes.Unbox
                : OpCodes.Castclass, targetType);

            var targetGetMethod = propertyInfo.GetGetMethod();
            var opCode = targetType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
            ilCommunication.Emit(opCode, targetGetMethod);
            var returnType = targetGetMethod.ReturnType;


            if (returnType.IsValueType)
            {
                ilCommunication.Emit(OpCodes.Box, returnType);
            }

            ilCommunication.Emit(OpCodes.Ret);

            var del = getMethod.CreateDelegate(Expression.GetFuncType(setParamType, setReturnType));
            return del as Func<Object, Object>;
        }

        private static readonly Type[] ParamTypes = new Type[]
        {
    typeof(object).MakeByRefType(), typeof(object)
        };

        PropertySetter ITypeManipulator.CreateSetMethod(MemberInfo memberInfo)
            => CreateSetMethod(memberInfo);

        /// <summary>
        /// Returns a delegate that can be invoked to quickly set the value for an object
        /// of targetType.  This method assumes this property has a setter. For properties
        /// without a setter use CreateReadOnlyPropertySetter
        /// </summary>
        private static PropertySetter CreateSetMethod(MemberInfo memberInfo)
        {
            Type paramType;
            switch (memberInfo)
            {
                case PropertyInfo info:
                    paramType = info.PropertyType;
                    break;
                case FieldInfo info:
                    paramType = info.FieldType;
                    break;
                default:
                    throw new Exception("Can only create set methods for properties and fields.");
            }

            var reflectedType = memberInfo.ReflectedType;
            var decType = memberInfo.DeclaringType;
            if (reflectedType == null || decType == null)
                throw new InvalidOperationException();

            var setter = new DynamicMethod(
                "",
                typeof(void),
                ParamTypes,
                reflectedType.Module,
                true);
            var generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldind_Ref);

            if (decType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox, decType);

            }
            generator.Emit(OpCodes.Ldarg_1);
            if (paramType.IsValueType)
                generator.Emit(OpCodes.Unbox_Any, paramType);

            switch (memberInfo)
            {
                case PropertyInfo info:
                    generator.Emit(OpCodes.Callvirt, info.GetSetMethod(true));
                    break;
                case FieldInfo field:
                    generator.Emit(OpCodes.Stfld, field);
                    break;
            }

            generator.Emit(OpCodes.Ret);

            return (PropertySetter)setter.CreateDelegate(typeof(PropertySetter));
        }

        IEnumerable<FieldInfo> ITypeManipulator.GetRecursivePrivateFields(Type type)
            => GetRecursivePrivateFields(type);

        public static IEnumerable<FieldInfo> GetRecursivePrivateFields(Type type)
        {
            while (true)
            {
                foreach (var field in type.GetFields(Const.NonPublic))
                    yield return field;

                var parent = type.BaseType;
                if (parent == null) yield break;

                type = parent;
            }
        }

        private static FieldInfo GetBackingField(PropertyInfo pi)
        {
            var compGen = typeof(CompilerGeneratedAttribute);

            var decType = pi?.DeclaringType;

            if (decType == null || !pi.CanRead || 
                !pi.GetGetMethod(true).IsDefined(compGen, true))
                return null;
            var backingField = decType.GetField($"<{pi.Name}>k__BackingField",
                Const.NonPublic);
            if (backingField == null)
                return null;
            if (backingField.IsDefined(compGen, inherit: true))
                return backingField;

            var flds = GetRecursivePrivateFields(decType).ToArray();

            if (flds.Length == 0)
                return null;
            var name = $"<{pi.Name}>";

            backingField = flds.FirstOrDefault(f => f.Name.Contains(name))
                ?? flds.FirstOrDefault(f => f.Name.IndexOf(name,
                    StringComparison.OrdinalIgnoreCase) >= 0);

            if (backingField == null || backingField.FieldType != pi.PropertyType)
                return null;

            return backingField;
        }

        public Boolean TryCreateReadOnlyPropertySetter(PropertyInfo propertyInfo,
            out Action<Object, Object> setter)
        {
            var backingField = GetBackingField(propertyInfo);
            if (backingField == null)
            {
                setter = default;
                return false;
            }
            setter = CreateFieldSetter(backingField);
            return setter != null;
        }

        public Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo)
        {
            var dynam = new DynamicMethod("", typeof(object), new[] { typeof(object) }
              , typeof(Func<Object, Object>), true);

            var il = dynam.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
            }
            else
            {
                il.Emit(OpCodes.Ldsfld, fieldInfo);
            }

            if (fieldInfo.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, fieldInfo.FieldType);
            }

            il.Emit(OpCodes.Ret);
            return (Func<Object, Object>)dynam.CreateDelegate(typeof(Func<Object, Object>));
        }

        public Action<Object, Object> CreateFieldSetter(FieldInfo fieldInfo)
        {
            var dynam = new DynamicMethod(
                ""
                , typeof(void)
                , new[] { typeof(object), typeof(object) }
                , typeof(VoidMethod)
                , true
            );

            var il = dynam.GetILGenerator();

            // If method isn't static push target instance on top
            // of stack.
            if (!fieldInfo.IsStatic)
            {
                // Argument 0 of dynamic method is target instance.
                il.Emit(OpCodes.Ldarg_0);
            }
            il.Emit(OpCodes.Ldarg_1);     // load value

            if (fieldInfo.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
            }

            if (!fieldInfo.IsStatic)
            {
                il.Emit(OpCodes.Stfld, fieldInfo); // store into field
            }
            else
            {
                il.Emit(OpCodes.Stsfld, fieldInfo); // static store into field
            }

         
            il.Emit(OpCodes.Ret);
            return (Action<Object, Object>)dynam.CreateDelegate(typeof(Action<Object, Object>));
        }


        public VoidMethod CreateMethodCaller(MethodInfo method)
        {
            var dyn = CreateMethodCaller(method, true);
            return (VoidMethod)dyn.CreateDelegate(typeof(VoidMethod));
        }

        public Func<Object, Object[], Object> CreateFuncCaller(MethodInfo method)
        {
            var dyn = CreateMethodCaller(method, false);
            return (Func<Object, Object[], Object>)dyn.CreateDelegate(typeof(Func<Object, Object[], Object>));
        }

        public DynamicMethod CreateMethodCaller(MethodInfo method, Boolean isSuppressReturnValue)
        {
            Type[] argTypes = { typeof(object), typeof(object[]) };
            var parms = method.GetParameters();

            var retType = isSuppressReturnValue ? typeof(void) :
                typeof(object);

            var dynam = new DynamicMethod(String.Empty, retType, argTypes
                , typeof(DasType), true);
            var il = dynam.GetILGenerator();

            //pass the target object.  If it's a struct (value type) we have to pass the address
            il.Emit(method.DeclaringType?.IsValueType == true ?
                OpCodes.Ldarga :
                OpCodes.Ldarg, 0);

            for (var i = 0; i < parms.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);

                var parmType = parms[i].ParameterType;
                if (parmType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, parmType);
                }
            }

            il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);

            if (method.ReturnType != typeof(void))
            {
                if (!isSuppressReturnValue)
                {
                    if (method.ReturnType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, method.ReturnType);
                    }
                }
                else
                    il.Emit(OpCodes.Pop);
            }

            il.Emit(OpCodes.Ret);

            return dynam;
        }

        /// <summary>
        /// Gets a delegate to add an object to a generic collection
        /// </summary>		
        public VoidMethod CreateAddDelegate<T>(IEnumerable<T> collection)
        {
            var colType = collection.GetType();

            if (CachedAdders.TryGetValue(colType, out var res))
                return res;

            var method = GetAddMethod(collection);
            if (method != null)
            {
                var dynam = CreateMethodCaller(method, true);
                res = (VoidMethod)dynam.CreateDelegate(typeof(VoidMethod));
            }
            CachedAdders.TryAdd(colType, res);

            return res;
        }

        /// <summary>
        /// Gets a delegate to add an object to a non-generic collection
        /// </summary>	
        public VoidMethod GetAdder(IEnumerable collection, Type type = null)
        {
            if (type == null)
                type = collection.GetType();

            if (CachedAdders.TryGetValue(type, out var res))
                return res;

            if (type.IsGenericType)
            {
                dynamic dCollection = collection;
                res = CreateAddDelegate(dCollection);
                //no need to cache here since it will be added to the cache in the other method
            }
            else if (collection is ICollection icol)
            {
                res = CreateAddDelegate(icol, type);
            }
            else
            {
                var boxList = new List<Object>(collection.OfType<Object>());
                res = CreateAddDelegate(boxList, type);
            }

            return res;
        }

        private VoidMethod CreateAddDelegate(ICollection collection, Type type)
        {
            var colType = collection.GetType();

            if (CachedAdders.TryGetValue(colType, out var res))
                return res;

            //super sophisticated
            var method = type.GetMethod("Add");
            if (method != null)
            {
                var dynam = CreateMethodCaller(method, true);
                res = (VoidMethod)dynam.CreateDelegate(typeof(VoidMethod));
            }
            CachedAdders.TryAdd(colType, res);

            return res;
        }

        /// <summary>
        /// Detects the Add, Enqueue, Push etc method for generic collections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        public MethodInfo GetAddMethod<T>(IEnumerable<T> collection)
        {
            var cType = collection.GetType();

            if (typeof(ICollection<T>).IsAssignableFrom(cType))
                return typeof(ICollection<T>).GetMethod("Add", new[] { typeof(T) });
            if (typeof(IList).IsAssignableFrom(cType))
                return typeof(IList).GetMethod("Add", new[] { typeof(T) });

            var prmType = cType.GetGenericArguments().FirstOrDefault()
                ?? typeof(Object);


            foreach (var meth in cType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (meth.ReturnType != typeof(void))
                    continue;
                var prms = meth.GetParameters();
                if (prms.Length != 1 || prms[0].ParameterType != prmType)
                    continue;

                return meth;
            }

            return null;
        }

        public Type GetPropertyType(Type classType, string propName)
        {
            if (propName == null)
                return default;

            var ts = GetStructure(classType, SerializationDepth.AllProperties);
            return ts.MemberTypes.TryGetValue(propName, out var res) ?
                InstanceMemberType(res) : default;
        }

       

        ITypeStructure ITypeManipulator.GetStructure(Type type, SerializationDepth depth)
            => GetStructure(type, depth);

        public ITypeStructure GetStructure<T>(SerializationDepth depth)
            => GetStructure(typeof(T), depth);

        public IEnumerable<MemberInfo> GetPropertiesToSerialize(Type type,
            SerializationDepth depth)
        {
            var str = GetStructure(type, depth);
            foreach (var pi in str.GetMembersToSerialize(depth))
                yield return pi;
        }

        public Type InstanceMemberType(MemberInfo info)
        {
            switch (info)
            {
                case PropertyInfo prop:
                    return prop.PropertyType;
                case FieldInfo field:
                    return field.FieldType;
                default:
                    throw new InvalidOperationException();
            }
        }

        public IEnumerable<MethodInfo> GetInterfaceMethods(Type type)
        {
            foreach (var parentInterface in type.GetInterfaces())
            {
                foreach (var pp in GetInterfaceMethods(parentInterface))
                {
                    yield return pp;
                }
            }

            foreach (var mi in type.GetMethods(InterfaceMethodBindings))
            {
                if (mi.IsPrivate)
                    continue;
                yield return mi;
            }
        }

        public int PropertyCount(Type type)
        {
            var str = ValidateCollection(type, Settings.SerializationDepth, true);
            return str.PropertyCount;
        }

        internal TypeStructure GetStructure(Type type, SerializationDepth depth)
        {
            if (Settings.IsPropertyNamesCaseSensitive)
                return ValidateCollection(type, depth, true);
            
            return ValidateCollection(type, depth, false);
        }

        private TypeStructure ValidateCollection(Type type, SerializationDepth depth,
                Boolean caseSensitive)
        {
            var collection = caseSensitive ? _knownSensitive : _knownInsensitive;

            var doCache = Settings.CacheTypeConstructors;

            if (doCache && collection.TryGetValue(type, out var result) && result.Depth >= depth)
                return result;

            result = new TypeStructure(type, caseSensitive, depth, this);
            if (!doCache)
                return result;

            return collection.AddOrUpdate(type, result, (k, v) => v.Depth > result.Depth ?
                v : result);
        }
    }
}
