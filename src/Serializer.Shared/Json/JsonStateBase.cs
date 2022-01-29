#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;
using Das.Serializer.Properties;
using Das.Serializer.State;

namespace Das.Serializer.Json
{
    public abstract class JsonStateBase : DynamicStateBase,
                                          IDynamicState<PropertyActor>
    {
        protected JsonStateBase(Type type,
                                ILGenerator il,
                                ITypeManipulator types,
                                Action<ILGenerator>? loadCurrentValueOntoStack,
                                FieldInfo invariantCulture,
                                IDictionary<Type, ProxiedInstanceField> proxies,
                                IEnumerable<PropertyActor> properties,
                                ISerializerSettings settings,
                                IFieldActionProvider actionProvider)
            : base(il, types, type, loadCurrentValueOntoStack, 
                proxies, actionProvider)
        {
            _invariantCulture = invariantCulture;
            _settings = settings;
            _currentPropertyIndex = 0;
            //var typeStruct = types.GetTypeStructure(type);
            _properties = properties.ToArray();
        }

        public override void LoadCurrentFieldValueToStack()
        {
            //if (!_types.TryGetNullableType(CurrentField.Type, out var baseType))
            {
                LoadParentToStack();

                //_il.Emit(OpCodes.Ldarg_1);
                var call = ParentType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
                _il.Emit(call, CurrentField.GetMethod);
            }
            //else
            //{
            //    LoadNullableFieldValueToStack();
            //}
        }

        PropertyActor IDynamicState<PropertyActor>.CurrentField => _properties[_currentPropertyIndex];

        public override FieldAction CurrentFieldAction => _properties[_currentPropertyIndex].FieldAction;
        //GetProtoFieldAction(CurrentField.Type);


        public override IPropertyInfo CurrentField => _properties[_currentPropertyIndex];

        protected Int32 _currentPropertyIndex;
        protected readonly FieldInfo _invariantCulture;
        protected readonly ISerializerSettings _settings;

        protected readonly PropertyActor[] _properties;
    }
}


#endif