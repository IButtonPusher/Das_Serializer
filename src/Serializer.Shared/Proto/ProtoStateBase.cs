#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;
using Das.Serializer.State;

namespace Das.Serializer.ProtoBuf
{
    public abstract class ProtoStateBase : DynamicStateBase,
                                           IDynamicState<IProtoFieldAccessor>
    {
        protected ProtoStateBase(ILGenerator il,
                                 IProtoFieldAccessor currentField,
                                 Type parentType,
                                 Action<ILGenerator>? loadCurrentValueOntoStack,
                                 IDictionary<Type, ProxiedInstanceField> proxies,
                                 ITypeManipulator types,
                                 IFieldActionProvider actionProvider)
            : base(il, types, parentType, loadCurrentValueOntoStack,
                proxies, actionProvider)
        {
            _currentField = currentField;
        }

        IProtoFieldAccessor IDynamicState<IProtoFieldAccessor>.CurrentField => _currentField;

        public override void LoadCurrentFieldValueToStack()
        {
            LoadParentToStack();
            var call = ParentType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
            IL.Emit(call, CurrentField.GetMethod);
        }

        public override IPropertyInfo CurrentField => _currentField;

        public override FieldAction CurrentFieldAction => _currentField.FieldAction;


        protected IProtoFieldAccessor _currentField;
    }
}

#endif
