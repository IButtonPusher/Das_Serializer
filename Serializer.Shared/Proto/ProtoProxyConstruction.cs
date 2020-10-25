using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Types;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
        where TPropertyAttribute : Attribute
    {
        private ConstructorInfo AddConstructors(TypeBuilder bldr,
                                                ConstructorInfo dtoCtor,
                                                Type genericBase,
                                                ICollection<FieldBuilder> childProxies,
                                                Boolean isDtoReadOnly)
        {
            var baseCtors = genericBase.GetConstructors(BindingFlags.Public |
                                                        BindingFlags.NonPublic |
                                                        BindingFlags.Instance |
                                                        BindingFlags.FlattenHierarchy);

            DasTypeBuilder.CreateProperty(bldr, nameof(ProtoDynamicBase.IsReadOnly),
                typeof(Boolean), false, out var fieldInfo);

            foreach (var ctor in baseCtors)
                BuildOverrideConstructor(ctor, bldr,
                    dtoCtor, fieldInfo, childProxies, isDtoReadOnly);

            return dtoCtor;
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
            {
                il.Emit(OpCodes.Newobj, ctor);
            }
            else
            {
                // instantiate via private constructor = emit reflection =\

                var genericArg = il.DeclareLocal(typeof(Type));
                var localType = il.DeclareLocal(typeof(Type));
                var ctorType = il.DeclareLocal(typeof(ConstructorInfo));

                var baseTypeGetter = typeof(Type).GetterOrDie(nameof(Type.BaseType), out _);

                il.Emit(OpCodes.Ldarg_0);
                var getTypeMethod = typeof(Object).GetMethodOrDie(nameof(GetType));
                il.Emit(OpCodes.Callvirt, getTypeMethod);
                il.Emit(OpCodes.Stloc, localType);

                il.Emit(OpCodes.Ldloc, localType);
                il.Emit(OpCodes.Callvirt, baseTypeGetter);
                il.Emit(OpCodes.Stloc, localType);

                var getArgs = typeof(Type).GetMethodOrDie(nameof(Type.GetGenericArguments));

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
                    }) ?? throw new InvalidOperationException();

                var emptyFields = typeof(Type).GetField(nameof(Type.EmptyTypes))
                                  ?? throw new InvalidOperationException();

                var nonPublicField = typeof(ProtoDynamicBase).GetField(
                                         nameof(ProtoDynamicBase.InstanceNonPublic))
                                     ?? throw new InvalidOperationException();

                il.Emit(OpCodes.Ldloc, genericArg);
                il.Emit(OpCodes.Ldsfld, nonPublicField);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldsfld, emptyFields);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Callvirt, gettCtor);
                il.Emit(OpCodes.Stloc, ctorType);

                var invoke = typeof(ConstructorInfo).GetMethod(nameof(ConstructorInfo.Invoke),
                    new[] {typeof(Object[])}) ?? throw new InvalidOperationException();

                il.Emit(OpCodes.Ldloc, ctorType);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Call, invoke);
            }


            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private void BuildOverrideConstructor(
            ConstructorInfo baseCtor,
            TypeBuilder builder,
            ConstructorInfo dtoCtor, FieldInfo readOnlyBackingField,
            IEnumerable<FieldBuilder> childProxies,
            Boolean isDtoReadOnly)
        {
            var paramList = new List<Type>();
            paramList.AddRange(baseCtor.GetParameters().Select(p => p.ParameterType));

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

                var gargs = kvp.FieldType.GetGenericArguments();

                if (gargs.Length != 1)
                    throw new InvalidOperationException(
                        $"{kvp.FieldType} should have exactly one generic argument");

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


                var getProxyInstance = getProxyMethod.MakeGenericMethod(garg);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, _proxyProviderField);
                il.Emit(isProxyReadOnly);
                il.Emit(OpCodes.Callvirt, getProxyInstance);


                il.Emit(OpCodes.Stfld, kvp);
            }

            il.Emit(OpCodes.Ldarg_0);

            il.Emit(allowReadOnly);
            il.Emit(OpCodes.Stfld, readOnlyBackingField);

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Nop);

            il.Emit(OpCodes.Ret);
        }

        private static FieldBuilder CreateLocalProxy(IProtoFieldAccessor field, TypeBuilder builder,
                                                     Type germane)
        {
            var proxyType = typeof(IProtoProxy<>).MakeGenericType(germane);

            return builder.DefineField($"_{field.Name}Proxy", proxyType, FieldAttributes.Private);
        }

        private Dictionary<Type, FieldBuilder> CreateProxyFields(
            TypeBuilder bldr,
            IEnumerable<IProtoFieldAccessor> fields)
        {
            var typeProxies = new Dictionary<Type, FieldBuilder>();

            foreach (var field in fields)
                switch (field.FieldAction)
                {
                    case ProtoFieldAction.ChildObject:
                        if (typeProxies.ContainsKey(field.Type))
                            continue;

                        var local = CreateLocalProxy(field, bldr, field.Type);
                        typeProxies[field.Type] = local;
                        break;

                    case ProtoFieldAction.ChildObjectArray:
                    case ProtoFieldAction.ChildObjectCollection:
                    case ProtoFieldAction.Dictionary:
                        var germane = _types.GetGermaneType(field.Type);

                        if (typeProxies.ContainsKey(germane))
                            continue;

                        var bldr2 = CreateLocalProxy(field, bldr, germane);

                        typeProxies[germane] = bldr2;

                        break;
                }

            return typeProxies;
        }
    }
}