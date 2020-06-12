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
        
        private Dictionary<IProtoFieldAccessor, FieldBuilder> CreateProxyFields(
            TypeBuilder bldr,
            IEnumerable<IProtoFieldAccessor> fields)
        {
            var childProxies = new Dictionary<IProtoFieldAccessor, FieldBuilder>();

            foreach (var field in fields)
            {
                switch (field.FieldAction)
                {
                    case ProtoFieldAction.ChildObject:
                        var local = CreateLocalProxy(field, bldr, field.Type);

                        childProxies[field] = local;
                        break;

                    case ProtoFieldAction.ChildObjectArray:
                    case ProtoFieldAction.ChildObjectCollection:
                    case ProtoFieldAction.Dictionary:
                        var germane = _types.GetGermaneType(field.Type);
                        var bldr2 = CreateLocalProxy(field, bldr, germane);

                        childProxies[field] = bldr2;

                        break;
                }
            }

            return childProxies;
        }

        private static FieldBuilder CreateLocalProxy(IProtoFieldAccessor field, TypeBuilder builder, Type germane)
        {
            var proxyType = typeof(IProtoProxy<>).MakeGenericType(germane);

            return builder.DefineField($"_{field.Name}Proxy", proxyType, FieldAttributes.Private);


            //var localProxyRef = il.DeclareLocal(proxyType);

            //var getProxyInstance = _getProtoProxy.MakeGenericMethod(germane);
            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldfld, _proxyProviderField);
            //il.Emit(OpCodes.Ldc_I4_0);
            //il.Emit(OpCodes.Callvirt, getProxyInstance);


            //il.Emit(OpCodes.Stloc, localProxyRef);



            //return localProxyRef;
        }

        private ConstructorInfo AddConstructors(TypeBuilder bldr, 
            ConstructorInfo dtoCtor,
            Type genericBase,
            Dictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
            Boolean isDtoReadOnly)
        {
           

            var baseCtors = genericBase.GetConstructors(BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Instance |
                                                        BindingFlags.FlattenHierarchy);

            DasTypeBuilder.CreateProperty(bldr, nameof(ProtoDynamicBase.IsReadOnly),
                typeof(Boolean), false, out var fieldInfo);

            foreach (var ctor in baseCtors)
                BuildOverrideConstructor(ctor, bldr, //utfField,
                    dtoCtor, fieldInfo, childProxies, isDtoReadOnly);

            return dtoCtor;
        }

        private void BuildOverrideConstructor(ConstructorInfo baseCtor, TypeBuilder builder,
            //FieldInfo utfField, 
            ConstructorInfo dtoCtor, FieldInfo readOnlyBackingField,
            Dictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
            Boolean isDtoReadOnly)
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

            var allowReadOnly = dtoCtor == null ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;


            /////////////////////////////////
            // INSTANTIATE CHILD PROXY FIELDS
            /////////////////////////////////
            foreach (var kvp in childProxies)
            {
                il.Emit(OpCodes.Ldarg_0);

                //var germane = _types.GetGermaneType(kvp.Value.FieldType);

                var gargs = kvp.Value.FieldType.GetGenericArguments();

                if (gargs.Length != 1)
                    throw new InvalidOperationException(
                        $"{kvp.Value.FieldType} should have exactly one generic argument");

                var garg = gargs[0];

                var childHasEmptyCtor = _types.TryGetEmptyConstructor(garg, out _);

                OpCode isProxyReadOnly;
                MethodInfo getProxyMethod;

                switch (isDtoReadOnly)
                {
                    case false when childHasEmptyCtor:
                        isProxyReadOnly = OpCodes.Ldc_I4_0;
                        getProxyMethod = _getProtoProxy;
                        break;

                    case false when !childHasEmptyCtor:
                        isProxyReadOnly = OpCodes.Ldc_I4_0;
                        getProxyMethod = _getAutoProtoProxy;
                        break;

                    case true when childHasEmptyCtor:
                        isProxyReadOnly = OpCodes.Ldc_I4_1;
                        getProxyMethod = _getProtoProxy;
                        break;

                    case true when !childHasEmptyCtor:
                        isProxyReadOnly = OpCodes.Ldc_I4_1;
                        getProxyMethod = _getAutoProtoProxy;
                        break;

                    default:
                        throw new NotImplementedException();
                }
                
               


                //isProxyReadOnly = _types.TryGetEmptyConstructor(garg, out _)
                //    ? OpCodes.Ldc_I4_0
                //    : OpCodes.Ldc_I4_1;

                var getProxyInstance = getProxyMethod.MakeGenericMethod(garg);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, _proxyProviderField);
                il.Emit(isProxyReadOnly);
                il.Emit(OpCodes.Callvirt, getProxyInstance);
            
                
                il.Emit(OpCodes.Stfld, kvp.Value);
            }


            //var getUtf8 = typeof(Encoding).GetterOrDie(nameof(Encoding.UTF8), out _,
            //    Const.PublicStatic);

            //var utf = il.DeclareLocal(typeof(Encoding));

            //il.Emit(OpCodes.Call, getUtf8);
            //il.Emit(OpCodes.Stloc, utf);

            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldloc, utf);
            //il.Emit(OpCodes.Stfld, utfField);


            il.Emit(OpCodes.Ldarg_0);

            il.Emit(allowReadOnly);
            il.Emit(OpCodes.Stfld, readOnlyBackingField);


            //foreach (var kvp in )


            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Nop);

            il.Emit(OpCodes.Ret);
        }

        private static void AddDtoInstantiator(Type parentType, TypeBuilder bldr,
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
                    new[]
                    {
                        typeof(BindingFlags), typeof(Binder), typeof(Type[]),
                        typeof(ParameterModifier[])
                    });

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
                    new Type[] {typeof(Object[])});

                il.Emit(OpCodes.Ldloc, ctorType);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Call, invoke);
            }


            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }
    }
}
