#if GENERATECODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.Properties;
using Das.Serializer.State;
using Reflection.Common;

namespace Das.Serializer.CodeGen
{
    public abstract class BaseDynamicProvider<TField, TReturns, TState> : IFieldActionProvider
        where TField : IPropertyInfo, IPropertyActionAware, IIndexedProperty
        where TState : IDynamicPrintState<TField, TReturns>
    {
        protected BaseDynamicProvider(ITypeManipulator types,
                                      IInstantiator instantiator)
        {
            _types = types;
            _instantiator = instantiator;

            var asmName = new AssemblyName("Das.RuntimeAssembly");
            // ReSharper disable once JoinDeclarationAndInitializer
            AssemblyBuilderAccess access;


            #if NET45 || NET40

            access = AssemblyBuilderAccess.RunAndSave;

            _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, access);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(AssemblyName, $"{AssemblyName}.dll");

            #else
            access = AssemblyBuilderAccess.Run;
            _asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, access);
            _moduleBuilder = _asmBuilder.DefineDynamicModule(AssemblyName);

            #endif
        }

        /// <summary>
        ///     Assumes everything is on the stack and only the corrent "write" method needs to be called
        /// </summary>
        public void AppendPrimitive(IDynamicPrintState s,
                                    TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Int32:
                    s.AppendInt32();
                    break;

                case TypeCode.Int64:
                    s.AppendInt64();
                    break;

                case TypeCode.UInt64:
                    s.AppendUInt64();
                    break;

                case TypeCode.Int16:
                    s.AppendInt16();
                    break;

                case TypeCode.Byte:
                    s.AppendInt8();
                    break;

                case TypeCode.Boolean:
                    s.AppendBoolean();
                    break;

                case TypeCode.UInt32:
                    s.AppendUInt32();
                    break;

                case TypeCode.Single:
                    s.AppendSingle();
                    break;

                case TypeCode.Double:
                    s.AppendDouble();
                    break;

                case TypeCode.Decimal:
                    s.AppendDecimal();
                    break;

                case TypeCode.DateTime:
                    s.AppendDateTime();
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public FieldAction GetProtoFieldAction(Type pType)
        {
            if (pType.IsPrimitive)
                return pType == typeof(Int32) ||
                       pType == typeof(UInt32) ||
                       pType == typeof(Int16) ||
                       pType == typeof(Int64) ||
                       pType == typeof(UInt64) ||
                       pType == typeof(Byte) ||
                       pType == typeof(Boolean)
                    ? FieldAction.VarInt
                    : FieldAction.Primitive;

            if (pType.IsEnum)
            {
                return FieldAction.Enum;
            }

            var typeCode = Type.GetTypeCode(pType);

            switch (typeCode)
            {
                case TypeCode.Empty:
                    break;
                case TypeCode.Object:
                    break;
                case TypeCode.DBNull:
                    break;
                case TypeCode.Boolean:
                    break;
                case TypeCode.Char:
                    break;
                case TypeCode.SByte:
                    break;
                case TypeCode.Byte:
                    break;
                case TypeCode.Int16:
                    break;
                case TypeCode.UInt16:
                    break;
                case TypeCode.Int32:
                    break;
                case TypeCode.UInt32:
                    break;
                case TypeCode.Int64:
                    break;
                case TypeCode.UInt64:
                    break;
                case TypeCode.Single:
                    break;
                case TypeCode.Double:
                    break;
                case TypeCode.Decimal:
                    return FieldAction.Primitive;

                case TypeCode.DateTime:
                    return FieldAction.DateTime;

                case TypeCode.String:
                    return FieldAction.String;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (pType == Const.StrType)
                return FieldAction.String;

            if (pType == Const.ByteArrayType)
                return FieldAction.ByteArray;

            if (GetPackedArrayTypeCode(pType) != TypeCode.Empty)
                return FieldAction.PackedArray;

            if (typeof(IDictionary).IsAssignableFrom(pType))
                return FieldAction.Dictionary;

            if (_types.IsCollection(pType))
            {
                var germane = _types.GetGermaneType(pType);

                var subAction = GetProtoFieldAction(germane);

                switch (subAction)
                {
                    case FieldAction.ChildObject:
                        return pType.IsArray
                            ? FieldAction.ChildObjectArray
                            : FieldAction.ChildObjectCollection;

                    default:
                        return pType.IsArray
                            ? FieldAction.ChildPrimitiveArray
                            : FieldAction.ChildPrimitiveCollection;
                }
            }

            if (_types.TryGetNullableType(pType, out _))
            {
                return FieldAction.NullableValueType;
            }

            if (_types.IsLeaf(pType, true))
                return FieldAction.Primitive;

            if (pType.IsSerializable && !pType.IsGenericType &&
                !typeof(IStructuralEquatable).IsAssignableFrom(pType))
                return FieldAction.FallbackSerializable;

            if (TryGetSpecialProperty(pType, out _))
                return FieldAction.HasSpecialProperty;

            return FieldAction.ChildObject;
        }

        public virtual void DumpProxies()
        {
        #if DEBUG 
        #if NET40 || NET45
            _asmBuilder.Save("protoTest.dll");
        #endif
        #endif
        }

        protected abstract Boolean TryGetFieldAccessor(PropertyInfo prop,
                                                       Boolean isRequireAttribute,
                                                       GetFieldIndex getFieldIndex,
                                                       Int32 lastIndex,
                                                       out TField field);

        protected abstract Type GetProxyClosedGenericType(Type argType);

        protected void AddFieldToPrintMethod(TState state)
        {
            AddFieldToPrintMethod(state, state.CurrentFieldAction);
        }

        protected void AddFieldToPrintMethod(TState state,
                                             FieldAction fieldAction)
        {
            var ifFalse2 = state.VerifyShouldPrintValue();

            switch (fieldAction)
            {
                case FieldAction.VarInt:
                    PrintVarInt(state);
                    break;

                case FieldAction.Primitive:
                    PrintPrimitive(state);
                    break;

                case FieldAction.String:
                    state.PrintStringField();
                    break;

                case FieldAction.ByteArray:
                    state.PrintByteArrayField();
                    break;

                case FieldAction.PackedArray:
                    state.PrintIntCollection();
                    break;

                case FieldAction.ChildObject:
                    state.PrintChildObjectField(state.LoadCurrentFieldValueToStack,
                        state.CurrentField.Type);
                    break;

                case FieldAction.ChildObjectCollection:
                    state.PrintObjectCollection();
                    break;

                case FieldAction.ChildPrimitiveCollection:
                    state.PrintPrimitiveCollection();
                    break;

                case FieldAction.Dictionary:
                    state.PrintDictionary();
                    break;

                case FieldAction.ChildObjectArray:
                    state.PrintObjectArray();
                    break;

                case FieldAction.ChildPrimitiveArray:
                    state.PrintPrimitiveArray();
                    break;

                case FieldAction.DateTime:
                    state.PrintDateTimeField();
                    break;

                case FieldAction.NullableValueType:
                    PrintNullableValueType(state);

                    break;

                case FieldAction.HasSpecialProperty:
                    if (!TryGetSpecialProperty(state.CurrentField.Type, out var propInfo))
                        throw new InvalidOperationException();

                    state.PrintCurrentFieldHeader();

                    state.PrepareToPrintValue(propInfo,
                        (s,
                         p) =>
                        {
                            if (s.CurrentField.Type.IsValueType)
                            {
                                var local = s.GetLocal(s.CurrentField.Type);

                                s.LoadCurrentFieldValueToStack();

                                s.IL.Emit(OpCodes.Stloc, local);
                                s.IL.Emit(OpCodes.Ldloca, local);
                                s.IL.Emit(OpCodes.Call, p.GetGetMethod());
                            }
                            else
                            {
                                s.LoadCurrentFieldValueToStack();
                                s.IL.Emit(OpCodes.Callvirt, p.GetGetMethod());
                            }
                        });

                    break;

                case FieldAction.Enum:
                    state.PrintCurrentFieldHeader();
                    state.PrintEnum();
                    break;

                case FieldAction.FallbackSerializable:
                    state.PrintFallback();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            state.IL.MarkLabel(ifFalse2);
        }

        protected static void LoadReferenceDto(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_1);
        }

        protected static void LoadValueDto(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarga, 1);
        }

        protected List<TField> GetPrintFields(Type type)
        {
            var res = new List<TField>();
            var lastIndex = -1;
            foreach (var prop in _types.GetPublicProperties(type, false))
            {
                if (TryGetFieldAccessor(prop, true, GetIndexFromAttribute,
                        lastIndex, out var protoField))
                {
                    lastIndex = protoField.Index;
                    res.Add(protoField);
                }
            }

            return res;
        }

        protected List<TField> GetProtoScanFields(Type type,
                                                  out ConstructorInfo useCtor)
        {
            _instantiator.TryGetDefaultConstructor(type, out var emptyCtor);
            var hasPropCtor = _instantiator.TryGetPropertiesConstructor(type, out var propCtor);

            var ctorParamNames = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);
            if (hasPropCtor)
                foreach (var prm in propCtor.GetParameters())
                {
                    if (String.IsNullOrEmpty(prm.Name))
                        continue;

                    ctorParamNames.Add(prm.Name, prm.ParameterType);
                }


            var useProperties = new List<PropertyInfo>();

            foreach (var prop in _types.GetPublicProperties(type, false))
            {
                var hasSetter = prop.GetSetMethod(true) != null;
                if (hasSetter)
                    useProperties.Add(prop);
                else
                {
                    if (ctorParamNames.TryGetValue(prop.Name, out var ctorArgType)
                        && ctorArgType == prop.PropertyType)
                        useProperties.Add(prop);
                }
            }

            var protoFields = new List<TField>();

            var lastIndex = 0;

            for (var c = 0; c < useProperties.Count; c++)
            {
                var current = useProperties[c];

                var next = c + 1;

                if (TryGetFieldAccessor(current, true, (_,
                                                        _) => next, lastIndex,
                        out var protoField))
                {
                    lastIndex = protoField.Index;
                    protoFields.Add(protoField);
                }
            }

            useCtor = emptyCtor ?? propCtor;
            return protoFields;
        }

        protected Dictionary<Type, ProxiedInstanceField> CreateProxyFields(TypeBuilder bldr,
                                                                           IEnumerable<TField> fields)
        {
            var typeProxies = new Dictionary<Type, ProxiedInstanceField>();

            foreach (var field in fields)
            {
                switch (field.FieldAction)
                {
                    case FieldAction.ChildObject:
                        if (typeProxies.ContainsKey(field.Type))
                            continue;

                        var local = CreateLocalProxy(field, bldr, field.Type);
                        typeProxies[field.Type] = local;
                        break;

                    case FieldAction.ChildObjectArray:
                    case FieldAction.ChildObjectCollection:
                    case FieldAction.Dictionary:
                        var germane = _types.GetGermaneType(field.Type);

                        if (typeProxies.ContainsKey(germane))
                            continue;

                        var bldr2 = CreateLocalProxy(field, bldr, germane);

                        typeProxies[germane] = bldr2;

                        break;
                }
            }

            return typeProxies;
        }


        public Boolean TryGetSpecialProperty(Type pType,
                                                out PropertyInfo propInfo)
        {
            foreach (var ctor in pType.GetConstructors())
            {
                var ctorParams = ctor.GetParameters();
                if (ctorParams.Length != 1)
                    continue;

                var ctorParam = ctorParams[0];
                if (!ctorParam.ParameterType.IsPrimitive)
                    continue;

                var prop = pType.GetProperties()
                                .FirstOrDefault(p =>
                                    String.Equals(p.Name, ctorParam.Name,
                                        StringComparison.OrdinalIgnoreCase));

                if (prop != null)
                {
                    propInfo = prop;
                    return true;
                }
            }

            propInfo = default!;
            return false;
        }

        protected abstract Int32 GetIndexFromAttribute(PropertyInfo prop,
                                                       Int32 lastIndex);

        private void PrintPrimitive(TState s)
        {
            s.PrintCurrentFieldHeader();
            s.PrepareToPrintValue();

            AppendPrimitive(s, s.CurrentField.TypeCode);
        }

        private void PrintVarInt(TState s)
        {
            s.PrintCurrentFieldHeader();
            s.PrepareToPrintValue();

            AppendPrimitive(s, s.CurrentField.TypeCode);
        }

        private void PrintNullableValueType(TState s)
        {
            s.PrintCurrentFieldHeader();

            var nullableType = s.CurrentField.Type;
            var il = s.IL;

            if (!_types.TryGetNullableType(nullableType, out var baseType))
                throw new InvalidOperationException();

            var tmpVal = s.GetLocal(nullableType);


            var getHasValue = nullableType.GetPropertyGetterOrDie(
                nameof(Nullable<Int32>.HasValue));

            var ifNull = s.IL.DefineLabel();
            var eof = s.IL.DefineLabel();

            s.LoadCurrentFieldValueToStack();
            il.Emit(OpCodes.Stloc, tmpVal);
            il.Emit(OpCodes.Ldloca, tmpVal);

            il.Emit(OpCodes.Call, getHasValue);

            il.Emit(OpCodes.Brfalse, ifNull);

            //not null

            ///////////////////////////////////////////////

            s.AppendPrimitive(tmpVal, Type.GetTypeCode(baseType), (state,
                                                                   tv) =>
            {
                var getValue = nullableType.GetPropertyGetterOrDie(
                    nameof(Nullable<Int32>.Value));

                state.IL.Emit(OpCodes.Ldloca, tv);

                state.IL.Emit(OpCodes.Call, getValue);

                
            });

            /////////////////////////////////////

            il.Emit(OpCodes.Br, eof);

            il.MarkLabel(ifNull);

            // null
            s.AppendNull();

            il.MarkLabel(eof);
        
        }

        private ProxiedInstanceField CreateLocalProxy(INamedField field,
                                                      TypeBuilder builder,
                                                      Type germane)
        {
            var proxyType = GetProxyClosedGenericType(germane);

            var fieldInfo = builder.DefineField($"_{field.Name}Proxy", proxyType, FieldAttributes.Private);
            return new ProxiedInstanceField(proxyType, fieldInfo,
                proxyType.GetMethodOrDie("Print"));
        }

        private static TypeCode GetPackedArrayTypeCode(Type propertyType)
        {
            if (typeof(IEnumerable<Int32>).IsAssignableFrom(propertyType))
                return TypeCode.Int32;

            if (typeof(IEnumerable<Int16>).IsAssignableFrom(propertyType))
                return TypeCode.Int16;

            return typeof(IEnumerable<Int64>).IsAssignableFrom(propertyType)
                ? TypeCode.Int64
                : TypeCode.Empty;
        }

        private const string AssemblyName = "BOB.Stuff";

        //private static readonly String SaveFile = $"{AssemblyName}.dll";
        protected readonly AssemblyBuilder _asmBuilder;

        protected readonly IInstantiator _instantiator;
        protected readonly ModuleBuilder _moduleBuilder;

        protected readonly ITypeManipulator _types;

        protected abstract MethodInfo GetProxyMethod { get; }
    }
}

#endif
