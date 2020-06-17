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

            ChildProxies = childProxies;
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
            var lode = LoadCurrentValueOntoStack ?? throw new NullReferenceException(nameof(LoadCurrentValueOntoStack));

            lode(_il);
        }

        protected readonly ILGenerator _il;
        private readonly IDictionary<Type, FieldBuilder> _proxies;
    }
}