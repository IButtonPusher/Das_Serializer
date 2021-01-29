#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public abstract class ProtoStateBase
    {
        protected ProtoStateBase(ILGenerator il,
                                 IProtoFieldAccessor currentField,
                                 Type parentType,
                                 Action<ILGenerator>? loadCurrentValueOntoStack,
                                 IDictionary<Type, FieldBuilder> proxies,
                                 ITypeCore types)
        {
            _il = il;
            _proxies = proxies;
            _types = types;

            CurrentField = currentField;
            ParentType = parentType;
            LoadCurrentValueOntoStack = loadCurrentValueOntoStack;
        }

        public IProtoFieldAccessor CurrentField { get; set; }

        public ILGenerator IL => _il;

        public Action<ILGenerator>? LoadCurrentValueOntoStack { get; }

        public Type ParentType { get; }


        public FieldBuilder GetProxy(Type type)
        {
            return _proxies[type];
        }


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
            var germane = _types.GetGermaneType(field.Type);

            var proxyField = GetProxy(germane);

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
        private readonly ITypeCore _types;
    }
}

#endif
