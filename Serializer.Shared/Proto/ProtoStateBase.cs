using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Das.Serializer.Proto
{
    public abstract class ProtoStateBase
    {
        protected ProtoStateBase(
            ILGenerator il,
            IProtoFieldAccessor currentField,
            IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
            Type parentType, 
            Action<ILGenerator>? loadCurrentValueOntoStack,
            IDictionary<Type, FieldBuilder> proxies)
        {
            _il = il;
            _proxies = proxies;

            CurrentField = currentField;

            ChildProxies = childProxies; // ?? new Dictionary<IProtoFieldAccessor, LocalBuilder>();
            ParentType = parentType;
            LoadCurrentValueOntoStack = loadCurrentValueOntoStack;
        }

        public IDictionary<IProtoFieldAccessor, FieldBuilder> ChildProxies { get; }


        
        public FieldBuilder GetProxy(Type type)
        {
            return _proxies[type];
        }

        public IProtoFieldAccessor CurrentField { get; set; }

        public ILGenerator IL => _il;

        public Action<ILGenerator>? LoadCurrentValueOntoStack { get; }

        public Type ParentType { get; }

        //private static LocalBuilder CreateLocalProxy(ILGenerator il, Type germane,
        //    MethodInfo _getProtoProxy, FieldInfo _proxyProviderField)
        //{
        //    var proxyType = typeof(IProtoProxy<>).MakeGenericType(germane);

        //    var localProxyRef = il.DeclareLocal(proxyType);

        //    var getProxyInstance = _getProtoProxy.MakeGenericMethod(germane);
        //    il.Emit(OpCodes.Ldarg_0);
        //    il.Emit(OpCodes.Ldfld, _proxyProviderField);
        //    il.Emit(OpCodes.Ldc_I4_0);
        //    il.Emit(OpCodes.Callvirt, getProxyInstance);

        //    //var localProxyRef = _proxyProvider.GetProtoProxy<T>(false);
        //    il.Emit(OpCodes.Stloc, localProxyRef);


        //    return localProxyRef;
        //}

        public void LoadCurrentFieldValueToStack()
        {
            LoadParentToStack();
            var call = ParentType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
            IL.Emit(call, CurrentField.GetMethod);
        }

        /// <summary>
        ///     Leaves the field on the stack
        /// </summary>
        public FieldInfo LoadFieldProxy(IProtoFieldAccessor field)
        {
            if (!ChildProxies.TryGetValue(field, out var proxyField))
                throw new KeyNotFoundException($"No proxy created for {proxyField}");

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldfld, proxyField);

            return proxyField;
        }

        public void LoadParentToStack()
        {
            LoadCurrentValueOntoStack(_il);
        }

        protected ILGenerator _il;
        private readonly IDictionary<Type, FieldBuilder> _proxies;
    }
}