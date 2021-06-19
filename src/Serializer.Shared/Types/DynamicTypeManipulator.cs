#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer
{
    public partial class TypeManipulator
    {
        /// <summary>
        ///     Returns a delegate that can be invoked to quickly get the value for an object
        ///     of targetType
        /// </summary>
        public Func<Object, Object> CreatePropertyGetter(Type targetType,
                                                         PropertyInfo propertyInfo)
        {
            return CreateDynamicPropertyGetter(targetType, propertyInfo);
        }

        public Func<TObject, TProperty> CreatePropertyGetter<TObject, TProperty>(PropertyInfo propInfo)
        {
            return CreateDynamicPropertyGetter<TObject, TProperty>(propInfo);
        }

       

        public Func<object, object> CreatePropertyGetter(Type targetType,
                                                         String propertyName,
                                                         out PropertyInfo propInfo)
        {
            return CreateDynamicPropertyGetter(targetType, propertyName, out propInfo);
        }


        public PropertySetter? CreateSetMethod(MemberInfo memberInfo)
        {
            var memberChain = new[] {memberInfo};
            return CreateSetterImpl<PropertySetter>(memberInfo.DeclaringType!, ParamTypes, memberChain);

            //return CreateSetMethodImpl(memberInfo);
        }

        public PropertySetter? CreateSetMethod(Type declaringType,
                                               String memberName)
        {
            return CreateDynamicSetter(declaringType, memberName);
        }

        public PropertySetter<T>? CreateSetMethod<T>(String memberName)
        {
            return CreateDynamicSetter<T>(memberName);
        }


        public Func<Object, Object> CreateFieldGetter(FieldInfo fieldInfo)
        {
            var dynam = new DynamicMethod(String.Empty, Const.ObjectType, Const.SingleObjectTypeArray
                , typeof(Func<Object, Object>), true);

            var il = dynam.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldInfo);
            }
            else
                il.Emit(OpCodes.Ldsfld, fieldInfo);

            if (fieldInfo.FieldType.IsValueType)
                il.Emit(OpCodes.Box, fieldInfo.FieldType);

            il.Emit(OpCodes.Ret);
            return (Func<Object, Object>) dynam.CreateDelegate(typeof(Func<Object, Object>));
        }

        public Func<TParent, TField> CreateFieldGetter<TParent, TField>(FieldInfo fieldInfo)
        {
            var dynam = new DynamicMethod(String.Empty, typeof(TField), 
                new[] { typeof(TParent)},
                typeof(Func<TParent, TField>), true);

            var il = dynam.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                
                //if (typeof(TField).IsValueType)
                //    il.Emit(OpCodes.Ldflda, fieldInfo);
                //else
                    il.Emit(OpCodes.Ldfld, fieldInfo);
            }
            else
                il.Emit(OpCodes.Ldsfld, fieldInfo);

            //if (fieldInfo.FieldType.IsValueType)
            //    il.Emit(OpCodes.Box, fieldInfo.FieldType);

            il.Emit(OpCodes.Ret);
            return (Func<TParent, TField>) dynam.CreateDelegate(typeof(Func<TParent, TField>));
        }

        public Action<Object, Object?> CreateFieldSetter(FieldInfo fieldInfo)
        {
            var dynam = new DynamicMethod(
                String.Empty
                , typeof(void)
                , Const.TwoObjectTypeArray
                , typeof(VoidMethod)
                , true
            );

            var il = dynam.GetILGenerator();

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            il.Emit(OpCodes.Ldarg_1);

            if (fieldInfo.FieldType.IsValueType) il.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);

            if (!fieldInfo.IsStatic)
                il.Emit(OpCodes.Stfld, fieldInfo);
            else
                il.Emit(OpCodes.Stsfld, fieldInfo);


            il.Emit(OpCodes.Ret);
            return (Action<Object, Object?>) dynam.CreateDelegate(typeof(Action<Object, Object?>));
        }




        public static VoidMethod CreateMethodCaller(MethodInfo method)
        {
            var dyn = CreateMethodCaller(method, true);
            return (VoidMethod) dyn.CreateDelegate(typeof(VoidMethod));
        }


        public static Func<object, object> CreateDynamicPropertyGetter(Type targetType,
                                                                       String propertyName,
                                                                       out PropertyInfo propInfo)
        {
            var propChainArr = GetPropertyChain(targetType, propertyName).ToArray();
            return CreateDynamicPropertyGetter(targetType, propChainArr, out propInfo);
        }

        public Func<TObject, TProperty> CreatePropertyGetter<TObject, TProperty>(String propertyName,
            out PropertyInfo propInfo)
        {
            var propChainArr = GetPropertyChain(typeof(TObject), propertyName).ToArray();
            return CreateDynamicPropertyGetter<TObject, TProperty>(propChainArr, out propInfo);
        }

        public static Func<TObject, TProperty> CreateDynamicPropertyGetter<TObject, TProperty>(
                                                                       PropertyInfo propertyInfo)
        {
            _singlePropFairy ??= new PropertyInfo[1];
            _singlePropFairy[0] = propertyInfo;
            return CreateDynamicPropertyGetter<TObject, TProperty>(_singlePropFairy, out _);
        }

        public static Func<Object, Object> CreateDynamicPropertyGetter(Type targetType,
                                                                       PropertyInfo propertyInfo)
        {
            _singlePropFairy ??= new PropertyInfo[1];
            _singlePropFairy[0] = propertyInfo;
            return CreateDynamicPropertyGetter(targetType, _singlePropFairy, out _);
        }

        ///// <summary>
        /////     Returns a delegate that can be invoked to quickly set the value for an object
        /////     of targetType.  This method assumes this property has a setter. For properties
        /////     without a setter use CreateReadOnlyPropertySetter
        ///// </summary>
        //private static PropertySetter CreateSetMethodImpl(MemberInfo memberInfo)
        //{
        //    Type paramType;

        //    switch (memberInfo)
        //    {
        //        case PropertyInfo info:
        //            paramType = info.PropertyType;
        //            break;
        //        case FieldInfo info:
        //            paramType = info.FieldType;
        //            break;
        //        default:
        //            throw new Exception("Can only create set methods for properties and fields.");
        //    }

        //    var reflectedType = memberInfo.ReflectedType;
        //    var decType = memberInfo.DeclaringType;
        //    if (reflectedType == null || decType == null)
        //        throw new InvalidOperationException();

        //    var setter = new DynamicMethod(
        //        String.Empty,
        //        typeof(void),
        //        ParamTypes,
        //        reflectedType.Module,
        //        true);
        //    var generator = setter.GetILGenerator();
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldind_Ref);

        //    if (decType.IsValueType)
        //        generator.Emit(OpCodes.Unbox, decType);

        //    generator.Emit(OpCodes.Ldarg_1);
        //    if (paramType.IsValueType)
        //        generator.Emit(OpCodes.Unbox_Any, paramType);

        //    switch (memberInfo)
        //    {
        //        case PropertyInfo info:
        //            generator.Emit(OpCodes.Callvirt, info.GetSetMethod(true)!);
        //            break;
        //        case FieldInfo field:
        //            generator.Emit(OpCodes.Stfld, field);
        //            break;
        //    }

        //    generator.Emit(OpCodes.Ret);

        //    return (PropertySetter) setter.CreateDelegate(typeof(PropertySetter));
        //}

        public static PropertySetter<T>? CreateDynamicSetter<T>(String memberName)
        {
            _paramTypeFairy ??= new Type[2];
            _paramTypeFairy[0] = typeof(T).MakeByRefType();
            _paramTypeFairy[1] = typeof(Object);

            //var propChainArr = GetPropertyChain(typeof(T), memberName).ToArray();
            var propChainArr = GetMemberChain(typeof(T), memberName).ToArray();

            return CreateSetterImpl<PropertySetter<T>>(typeof(T), _paramTypeFairy, propChainArr);
        }

        public static PropertySetter? CreateDynamicSetter(Type declaringType,
                                                          String memberName)
        {
            //var propChainArr = GetPropertyChain(declaringType, memberName).ToArray();
            var memChainArr = GetMemberChain(declaringType, memberName).ToArray();
            Array.Reverse(memChainArr);
            return CreateSetterImpl<PropertySetter>(declaringType, ParamTypes, memChainArr);

            //var propChainArr = GetPropertyChain(declaringType, memberName).ToArray();
            //return CreateSetterImpl<PropertySetter>(declaringType, ParamTypes, propChainArr);

            //var decType = declaringType;
            //if (decType == null)
            //    throw new InvalidOperationException();

            //var setter = new DynamicMethod(
            //    String.Empty,
            //    typeof(void),
            //    ParamTypes,
            //    declaringType.Module,
            //    true);
            //var generator = setter.GetILGenerator();
            //generator.Emit(OpCodes.Ldarg_0);
            //generator.Emit(OpCodes.Ldind_Ref);

            ////var propChainArr = GetPropertyChain(declaringType, memberName).ToArray();

            //for (var c = 0; c < propChainArr.Length; c++)
            //{
            //    var info = propChainArr[c];
            //    var accessCode = info.DeclaringType!.IsValueType ? OpCodes.Call : OpCodes.Callvirt;

            //    if (c == propChainArr.Length - 1)
            //    {
            //        // last stop => time to set

            //        if (!info.CanWrite)
            //            return null;

            //        var propSetter = info.GetSetMethod(true);
            //        if (propSetter == null)
            //            return null;

            //        Type paramType = info.PropertyType;

            //        if (decType!.IsValueType)
            //            generator.Emit(OpCodes.Unbox, decType);

            //        generator.Emit(OpCodes.Ldarg_1);
            //        if (paramType.IsValueType)
            //            generator.Emit(OpCodes.Unbox_Any, paramType);

            //        generator.Emit(accessCode, propSetter);
            //    }
            //    else
            //    {
            //        //time to get
            //        var targetGetMethod = info.GetGetMethod();

            //        generator.Emit(accessCode, targetGetMethod!);
            //    }

            //    decType = info.DeclaringType;
            //}

            //generator.Emit(OpCodes.Ret);

            //return (PropertySetter) setter.CreateDelegate(typeof(PropertySetter));
        }

        public static DynamicMethod CreateMethodCaller(MethodInfo method,
                                                       Boolean isSuppressReturnValue)
        {
            Type[] argTypes = {Const.ObjectType, typeof(Object[])};
            var parms = method.GetParameters();

            var retType = isSuppressReturnValue ? typeof(void) : Const.ObjectType;

            var dynam = new DynamicMethod(String.Empty, retType, argTypes
                , typeof(DasType), true);
            var il = dynam.GetILGenerator();

            //pass the target object.  If it's a struct (value type) we have to pass the address
            il.Emit(method.DeclaringType?.IsValueType == true ? OpCodes.Ldarga : OpCodes.Ldarg, 0);

            for (var i = 0; i < parms.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);

                var parmType = parms[i].ParameterType;
                if (parmType.IsValueType) il.Emit(OpCodes.Unbox_Any, parmType);
            }

            il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);

            if (method.ReturnType != typeof(void))
            {
                if (!isSuppressReturnValue)
                {
                    if (method.ReturnType.IsValueType) il.Emit(OpCodes.Box, method.ReturnType);
                }
                else
                    il.Emit(OpCodes.Pop);
            }

            il.Emit(OpCodes.Ret);

            return dynam;
        }


        private static Func<TObject, TProperty> CreateDynamicPropertyGetter<TObject, TProperty>(
                                                                        PropertyInfo[] propChainArr,
                                                                        out PropertyInfo propInfo)
        {
            var targetType = typeof(TObject);

            var setParamType = targetType;
            Type[] setParamTypes = {setParamType};
            var setReturnType = typeof(TProperty);

            var owner = typeof(DasType);

            var getMethod = new DynamicMethod(String.Empty, setReturnType,
                setParamTypes, owner, true);

            var il = getMethod.GetILGenerator();

            var ggLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);

            //il.Emit(targetType.IsValueType
            //    ? OpCodes.Unbox
            //    : OpCodes.Castclass, targetType);

            //////////////////

            MethodInfo? targetGetMethod = null;
            propInfo = default!;


            for (var c = 0; c < propChainArr.Length; c++)
            {
                propInfo = propChainArr[c];

                //put the chain of prop get results onto the stack
                targetGetMethod = propInfo.GetGetMethod();
                var opCode = propInfo.DeclaringType!.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
                il.Emit(opCode, targetGetMethod!);


                if (c >= propChainArr.Length - 1)
                    continue;

                // avoid calling property getters of null objects.  If we hit a null, return it
                var propLocal = il.DeclareLocal(propInfo.PropertyType);
                var nextLabel = il.DefineLabel();

                il.Emit(OpCodes.Stloc, propLocal);


                il.Emit(OpCodes.Ldloc, propLocal);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, nextLabel);

                il.Emit(OpCodes.Ldloc, propLocal);
                il.Emit(OpCodes.Br, ggLabel);


                il.MarkLabel(nextLabel);
                il.Emit(OpCodes.Ldloc, propLocal);
            }

            if (targetGetMethod == null)
                throw new InvalidOperationException();

            //var returnType = targetGetMethod.ReturnType;

            il.MarkLabel(ggLabel);

            //if (returnType.IsValueType)
            //    il.Emit(OpCodes.Box, returnType);

            il.Emit(OpCodes.Ret);

            var del = getMethod.CreateDelegate(Expression.GetFuncType(setParamType, setReturnType));
            return (Func<TObject, TProperty>) del;
        }


        private static Func<Object, Object> CreateDynamicPropertyGetter(Type targetType,
                                                                        PropertyInfo[] propChainArr,
                                                                        out PropertyInfo propInfo)
        {
            var setParamType = Const.ObjectType;
            Type[] setParamTypes = {setParamType};
            var setReturnType = Const.ObjectType;

            var owner = typeof(DasType);

            var getMethod = new DynamicMethod(String.Empty, setReturnType,
                setParamTypes, owner, true);

            var il = getMethod.GetILGenerator();

            var ggLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_0);

            il.Emit(targetType.IsValueType
                ? OpCodes.Unbox
                : OpCodes.Castclass, targetType);

            //////////////////

            MethodInfo? targetGetMethod = null;
            propInfo = default!;


            for (var c = 0; c < propChainArr.Length; c++)
            {
                propInfo = propChainArr[c];

                //put the chain of prop get results onto the stack
                targetGetMethod = propInfo.GetGetMethod();
                var opCode = propInfo.DeclaringType!.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
                il.Emit(opCode, targetGetMethod!);


                if (c >= propChainArr.Length - 1)
                    continue;

                // avoid calling property getters of null objects.  If we hit a null, return it
                var propLocal = il.DeclareLocal(propInfo.PropertyType);
                var nextLabel = il.DefineLabel();

                il.Emit(OpCodes.Stloc, propLocal);


                il.Emit(OpCodes.Ldloc, propLocal);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brfalse, nextLabel);

                il.Emit(OpCodes.Ldloc, propLocal);
                il.Emit(OpCodes.Br, ggLabel);


                il.MarkLabel(nextLabel);
                il.Emit(OpCodes.Ldloc, propLocal);
            }

            if (targetGetMethod == null)
                throw new InvalidOperationException();

            var returnType = targetGetMethod.ReturnType;

            il.MarkLabel(ggLabel);

            if (returnType.IsValueType)
                il.Emit(OpCodes.Box, returnType);

            il.Emit(OpCodes.Ret);

            var del = getMethod.CreateDelegate(Expression.GetFuncType(setParamType, setReturnType));
            return (Func<Object, Object>) del;
        }

        //private static TSetter? CreateSetterImpl<TSetter>(Type declaringType,
        //                                                  Type[] paramTypes,
        //                                                  PropertyInfo[] propChainArr)
        //    where TSetter : Delegate
        //{
        //    var decType = declaringType;
        //    if (decType == null)
        //        throw new InvalidOperationException();

        //    var setter = new DynamicMethod(
        //        String.Empty,
        //        typeof(void),
        //        paramTypes,
        //        declaringType.Module,
        //        true);
        //    var generator = setter.GetILGenerator();
        //    generator.Emit(OpCodes.Ldarg_0);
        //    generator.Emit(OpCodes.Ldind_Ref);

        //    for (var c = 0; c < propChainArr.Length; c++)
        //    {
        //        var info = propChainArr[c];
        //        var accessCode = info.DeclaringType!.IsValueType ? OpCodes.Call : OpCodes.Callvirt;

        //        if (c == propChainArr.Length - 1)
        //        {
        //            // last stop => time to set

        //            if (!info.CanWrite)
        //                return null;

        //            var propSetter = info.GetSetMethod(true);
        //            if (propSetter == null)
        //                return null;

        //            Type paramType = info.PropertyType;

        //            if (decType!.IsValueType)
        //                generator.Emit(OpCodes.Unbox, decType);

        //            generator.Emit(OpCodes.Ldarg_1);
        //            if (paramType.IsValueType)
        //                generator.Emit(OpCodes.Unbox_Any, paramType);

        //            generator.Emit(accessCode, propSetter);
        //        }
        //        else
        //        {
        //            //time to get
        //            var targetGetMethod = info.GetGetMethod();

        //            generator.Emit(accessCode, targetGetMethod!);
        //        }

        //        decType = info.DeclaringType;
        //    }

        //    generator.Emit(OpCodes.Ret);

        //    return (TSetter) setter.CreateDelegate(typeof(TSetter));
        //}

        private static TSetter? CreateSetterImpl<TSetter>(Type declaringType,
                                                          Type[] paramTypes,
                                                          MemberInfo[] propChainArr)
            where TSetter : Delegate
        {
            var decType = declaringType;
            if (decType == null)
                throw new InvalidOperationException();

            var setter = new DynamicMethod(
                String.Empty,
                typeof(void),
                paramTypes,
                declaringType.Module,
                true);
            var generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldind_Ref);

            for (var c = 0; c < propChainArr.Length; c++)
            {
                var info = propChainArr[c];
                var accessCode = info.DeclaringType!.IsValueType ? OpCodes.Call : OpCodes.Callvirt;

                if (c == propChainArr.Length - 1)
                {
                    // last stop => time to set
                    Type paramType;

                    switch (info)
                    {
                        case PropertyInfo prop:
                            if (!prop.CanWrite)
                                return null;
                            paramType = prop.PropertyType;

                            //generator.Emit(accessCode, prop.GetSetMethod(true)!);
                            break;

                        case FieldInfo field:
                            paramType = field.FieldType;

                            //generator.Emit(OpCodes.Stfld, field);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (decType!.IsValueType)
                        generator.Emit(OpCodes.Unbox, decType);

                    generator.Emit(OpCodes.Ldarg_1);
                    if (paramType.IsValueType)
                        generator.Emit(OpCodes.Unbox_Any, paramType);

                    switch (info)
                    {
                        case PropertyInfo prop:
                            generator.Emit(OpCodes.Callvirt, prop.GetSetMethod(true)!);
                            break;

                        case FieldInfo field:
                            generator.Emit(OpCodes.Stfld, field);
                            break;
                    }
                }
                else
                    //time to get

                    switch (info)
                    {
                        case PropertyInfo prop:
                            var targetGetMethod = prop.GetGetMethod();
                            generator.Emit(accessCode, targetGetMethod!);
                            break;

                        case FieldInfo fieldInfo:
                            if (!fieldInfo.IsStatic)
                            {
                                generator.Emit(OpCodes.Ldarg_0);
                                generator.Emit(OpCodes.Ldfld, fieldInfo);
                            }
                            else
                                generator.Emit(OpCodes.Ldsfld, fieldInfo);

                            if (fieldInfo.FieldType.IsValueType)
                                generator.Emit(OpCodes.Box, fieldInfo.FieldType);
                            break;
                    }

                //var targetGetMethod = info.GetGetMethod();

                //generator.Emit(accessCode, targetGetMethod!);

                decType = info.DeclaringType;
            }

            generator.Emit(OpCodes.Ret);

            return (TSetter) setter.CreateDelegate(typeof(TSetter));
        }

        private static IEnumerable<MemberInfo> GetMemberChain(Type declaringType,
                                                              String memberName)
        {
            if (!memberName.Contains("."))
            {
                foreach (var m in declaringType.GetMembersOrDie(memberName))
                {
                    yield return m;
                }

                yield break;
            }

            var subPropTokens = memberName.Split('.');

            foreach (var v in GetMemberChainImpl(declaringType, subPropTokens, 0))
            {
                yield return v;
            }
        }

        private static IEnumerable<MemberInfo> GetMemberChainImpl(Type declaringType,
                                                                  String[] subPropTokens,
                                                                  //MemberInfo[] memInfos,
                                                                  Int32 index)
        {
            if (declaringType == null ||
                !TryGetMembers(declaringType, subPropTokens[index], out var subMems))
                yield break;

            if (index == subPropTokens.Length - 1)
            {
                yield return subMems[0];
                yield break;
            }

            var hathFound = false;

            foreach (var subMem in subMems)
            {
                foreach (var v in GetMemberChainImpl(subMem.GetMemberType(), subPropTokens, //memInfos,
                    ++index))
                {
                    hathFound = true;
                    yield return v;
                }

                if (!hathFound)
                    continue;

                yield return subMem;
                yield break;
            }

            if (!hathFound)
                throw new MissingMemberException(declaringType.Name, subPropTokens[index]);
        }


        private static readonly Type[] ParamTypes =
        {
            Const.ObjectType.MakeByRefType(), Const.ObjectType
        };

       
    }
}

#endif
