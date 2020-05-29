using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Das.Extensions;
using Das.Types;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoDynamicProvider<TPropertyAttribute> : 
        IProtoProvider where TPropertyAttribute : Attribute
    {
        private static void AddDtoInstantiator<T>(Type parentType, TypeBuilder bldr, 
            Type genericParent, ConstructorInfo ctor)
        {
            var abstractMethod = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.BuildDefault));

            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.BuildDefault),
                MethodOverride, parentType, Type.EmptyTypes);

            var il = method.GetILGenerator();

            if (ctor.IsPublic)
                il.Emit(OpCodes.Newobj, ctor);
            else
            {
                // instantiate via private constructor

                var genericArg = il.DeclareLocal(typeof(Type));
                var localType = il.DeclareLocal(typeof(Type));
                var ctorType = il.DeclareLocal(typeof(ConstructorInfo));
                var baseTypeGetter = typeof(Type).GetProperty(nameof(Type.BaseType)).GetGetMethod();


                il.Emit(OpCodes.Ldarg_0);
                var getTypeMethod = typeof(Object).GetMethod(nameof(Object.GetType));
                il.Emit(OpCodes.Callvirt, getTypeMethod);
                il.Emit(OpCodes.Stloc, localType);

                il.Emit(OpCodes.Ldloc, localType);
                il.Emit(OpCodes.Callvirt, baseTypeGetter);
                il.Emit(OpCodes.Stloc, localType);

                var getArgs = typeof(Type).GetMethod(nameof(Type.GetGenericArguments));

                il.Emit(OpCodes.Ldloc, localType);
                il.Emit(OpCodes.Callvirt, getArgs);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldelem_Ref);
                il.Emit(OpCodes.Stloc, genericArg);


                var gettCtor = typeof(Type).GetMethod(nameof(Type.GetConstructor), 
                    new[] { typeof(BindingFlags), typeof(Binder), typeof(Type[]), 
                        typeof(ParameterModifier[])});

                var emptyFields = typeof(Type).GetField(nameof(Type.EmptyTypes));
                var nonPublicField = typeof(ProtoDynamicBase).GetField(
                    nameof(ProtoDynamicBase.InstanceNonPublic));

                il.Emit(OpCodes.Ldloc, genericArg);
                il.Emit(OpCodes.Ldsfld, nonPublicField);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldsfld, emptyFields);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Callvirt, gettCtor);
                il.Emit(OpCodes.Stloc, ctorType);

                var invoke = typeof(ConstructorInfo).GetMethod(nameof(ConstructorInfo.Invoke),
                    new Type[] { typeof(Object[])});

                il.Emit(OpCodes.Ldloc, ctorType);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Call, invoke);
            }


            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private static void AddConstructor(TypeBuilder bldr, FieldInfo utfField, Type genericBase,
            ConstructorInfo dtoCtor)
        {
            var baseCtors = genericBase.GetConstructors(BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Instance |
                                                        BindingFlags.FlattenHierarchy);

            var propBuilder = DasTypeBuilder.CreateProperty(bldr, nameof(ProtoDynamicBase.IsReadOnly),
                typeof(Boolean), out var fieldInfo, false);

            foreach (var ctor in baseCtors)
                BuildOverrideConstructor(ctor, bldr, utfField, dtoCtor, fieldInfo);
        }

        private static void BuildOverrideConstructor(ConstructorInfo baseCtor, TypeBuilder builder,
            FieldInfo utfField, ConstructorInfo dtoCtor, FieldInfo readOnlyBackingField)
        {

            var paramList = new List<Type>();
            paramList.AddRange(baseCtor.GetParameters().Select(p => p.ParameterType));

            //paramList.Add(typeof(IProtoProvider));
            
            var ctorParams = paramList.ToArray();

            var ctor = builder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                ctorParams);

            var il = ctor.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);

            il.Emit(OpCodes.Call, baseCtor);

            var getUtf8 = typeof(Encoding).GetterOrDie(nameof(Encoding.UTF8), out _,
                Const.PublicStatic);
               
            var utf = il.DeclareLocal(typeof(Encoding));

            il.Emit(OpCodes.Call, getUtf8);
            il.Emit(OpCodes.Stloc, utf);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, utf);
            il.Emit(OpCodes.Stfld, utfField);

            
            il.Emit(OpCodes.Ldarg_0);
            
            il.Emit(dtoCtor == null ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stfld, readOnlyBackingField);

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Nop);

            il.Emit(OpCodes.Ret);
        }
    }
}
