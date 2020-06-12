using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Das.Extensions;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public abstract class ProtoStateBase
    {
        public IDictionary<IProtoFieldAccessor, FieldBuilder> ChildProxies { get; }

        public ILGenerator IL => _il;
        public Type ParentType { get; }
        protected ILGenerator _il;

        protected  ProtoStateBase(ILGenerator il,
            ITypeCore typeCore, IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies, 
            Type parentType, Action<ILGenerator>? loadCurrentValueOntoStack)
        {
            _il = il;

            ChildProxies = childProxies;// ?? new Dictionary<IProtoFieldAccessor, LocalBuilder>();
            ParentType = parentType;
            LoadCurrentValueOntoStack = loadCurrentValueOntoStack;

            //var _getProtoProxy = typeof(IProtoProvider).GetMethod(nameof(IProtoProvider.GetProtoProxy));
            //var protoDynBase = typeof(ProtoDynamicBase);
            //var _proxyProviderField = protoDynBase.GetField("_proxyProvider", Const.NonPublic);

            //if (ChildProxies.Count > 0)
            //    return;

            //foreach (var field in fields)
            //{
            //    switch (field.FieldAction)
            //    {
            //        case ProtoFieldAction.ChildObject:
            //            var bldr = CreateLocalProxy(il, field.Type, 
            //                _getProtoProxy, _proxyProviderField);

            //            ChildProxies[field] = bldr;
            //            break;

            //        case ProtoFieldAction.ChildObjectArray:
            //        case ProtoFieldAction.ChildObjectCollection:
            //            var germane = typeCore.GetGermaneType(field.Type);
            //            var bldr2 = CreateLocalProxy(il, germane, 
            //                _getProtoProxy, _proxyProviderField);

            //            ChildProxies[field] = bldr2;

            //            break;
            //    }
            //}
        }

        public void LoadParentToStack()
        {
            //_loadObject(IL);
            LoadCurrentValueOntoStack(_il);
        }

        public void LoadCurrentFieldValueToStack()
        {
            LoadParentToStack();
            var call = ParentType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
            IL.Emit(call, CurrentField.GetMethod);
        }

        public Action<ILGenerator>? LoadCurrentValueOntoStack { get; }

        /// <summary>
        /// Leaves the field on the stack
        /// </summary>
        public FieldInfo LoadFieldProxy(IProtoFieldAccessor field)
        {
            if (!ChildProxies.TryGetValue(field, out var proxyField))
                throw new KeyNotFoundException($"No proxy created for {proxyField}");

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, proxyField);

            return proxyField;

        }


        public IProtoFieldAccessor? CurrentField { get; set; }

        private static LocalBuilder CreateLocalProxy(ILGenerator il, Type germane,
            MethodInfo _getProtoProxy, FieldInfo _proxyProviderField)
        {
            var proxyType = typeof(IProtoProxy<>).MakeGenericType(germane);

            var localProxyRef = il.DeclareLocal(proxyType);

            var getProxyInstance = _getProtoProxy.MakeGenericMethod(germane);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _proxyProviderField);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Callvirt, getProxyInstance);
            
            //var localProxyRef = _proxyProvider.GetProtoProxy<T>(false);
            il.Emit(OpCodes.Stloc, localProxyRef);

            //var streamSetter = proxyType.SetterOrDie(nameof(IProtoProxy<Object>.OutStream));


            //il.Emit(OpCodes.Ldloc, localProxyRef);
            
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Callvirt, streamSetter);

            return localProxyRef;
        }
    }
}
