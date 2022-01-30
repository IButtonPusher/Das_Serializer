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
            //ParentType = parentType;
        }

        //public override IPropertyInfo CurrentField => _currentField;

        IProtoFieldAccessor IDynamicState<IProtoFieldAccessor>.CurrentField => _currentField;

        public override void LoadCurrentFieldValueToStack()
        {
            LoadParentToStack();
            var call = ParentType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
            IL.Emit(call, CurrentField.GetMethod);

            
        }

        public override IPropertyInfo CurrentField => _currentField;

        public override FieldAction CurrentFieldAction => _currentField.FieldAction;


        ///// <summary>
        /////     Leaves the field on the stack
        ///// </summary>
        //public FieldInfo LoadFieldProxy(INamedField field)
        //{
        //    var germane = _types.GetGermaneType(field.Type);

        //    var proxy = GetProxy(germane);
        //    var proxyField = proxy.ProxyField;

        //    _il.Emit(OpCodes.Ldarg_0);
        //    _il.Emit(OpCodes.Ldfld, proxyField);

        //    return proxyField;
        //}


        //public Action<ILGenerator>? LoadCurrentValueOntoStack { get; }

        //public Type ParentType { get; }


        //public void LoadParentToStack()
        //{
        //    var lode = LoadCurrentValueOntoStack ?? throw new NullReferenceException(nameof(LoadCurrentValueOntoStack));

        //    lode(_il);
        //}

        //IProtoFieldAccessor IDynamicProtoState.CurrentField => _currentField;

        protected IProtoFieldAccessor _currentField;
    }
}

#endif
