#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;
using Das.Serializer.Json.Printers;
using Das.Serializer.Properties;
using Das.Serializer.Types;
using Reflection.Common;

namespace Das.Serializer.Printers
{
    public class DynamicJsonPrinterBuilder2 : DynamicPrinterBuilderBase2<String, PropertyActor, JsonPrintState>
    {
        public DynamicJsonPrinterBuilder2(ITypeInferrer typeInferrer,
                                          INodeTypeProvider nodeTypes,
                                          ITypeManipulator typeManipulator,
                                          IInstantiator instantiator)
            : base(typeInferrer, nodeTypes, typeManipulator, instantiator)
        {
        }

        protected override JsonPrintState GetInitialState(Type dtoType,
                                                          ILGenerator il,
                                                          Type tWriter,
                                                          FieldInfo invariantCulture,
                                                          ISerializerSettings settings,
                                                          IDictionary<Type, ProxiedInstanceField> typeProxies,
                                                          IEnumerable<PropertyActor> properties,
                                                          Dictionary<PropertyActor, FieldInfo> converterFields)
        {
            Action<ILGenerator> loadDto = dtoType.IsValueType
                ? LoadValueDto
                : LoadReferenceDto;

            var initialState = new JsonPrintState(dtoType, il, _types,
                loadDto, tWriter, invariantCulture, typeProxies,
                properties, settings, _typeInferrer, this, converterFields);

            return initialState;
        }

        protected override bool TryGetFieldAccessor(PropertyInfo prop,
                                                    Boolean isRequireAttribute,
                                                    GetFieldIndex getFieldIndex,
                                                    Int32 lastIndex,
                                                    out PropertyActor field)
        {
            if (prop.PropertyType is not { } propertyType)
            {
                field = default!;
                return false;
            }

            var setter = prop.CanWrite ? prop.GetSetMethod(true) : default!;
            var fieldAction = GetProtoFieldAction(propertyType);
            var index = getFieldIndex(prop, lastIndex);

            field = new PropertyActor(prop.Name, propertyType, prop.GetGetMethod(),
                setter, fieldAction, index);
            return true;
        }

        protected override Type GetProxyClosedGenericType(Type argType)
        {
            return typeof(ISerializerTypeProxy<>).MakeGenericType(argType);
        }

        protected override MethodInfo GetProxyMethod { get; } =
            typeof(IProxyProvider).GetMethodOrDie(nameof(IProxyProvider.GetJsonProxy));
    }
}


#endif
