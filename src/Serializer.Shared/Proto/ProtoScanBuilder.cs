#if GENERATECODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Serializer.CodeGen;
using Das.Serializer.Proto;
using Reflection.Common;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        /// <summary>
        ///     Puts the next field index on the stack.  Jumps to
        /// </summary>
        private static Label AddLoadCurrentFieldIndex(ILGenerator il,
                                                      Label goNextLabel)
        {
            /////////////////////////////
            // 1. GET CURRENT FIELD INDEX
            /////////////////////////////
            var nextFieldIndexLabel = il.DefineLabel();

            il.MarkLabel(nextFieldIndexLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getColumnIndex);
            il.Emit(OpCodes.Br, goNextLabel);

            return nextFieldIndexLabel;
        }

        /// <summary>
        ///     2.
        /// </summary>
        private void AddPropertiesToScanMethod(IProtoScanState state,
                                               Label afterPropertyLabel,
                                               Boolean canSetValuesInline,
                                               LocalBuilder streamLength,
                                               Object? exampleObject)
        {
            var il = state.IL;
            var fieldArr = state.Fields;

            if (!canSetValuesInline)
                state.EnsureLocalFields();

            var switchFieldIndexLabel = il.DefineLabel();
            var whileLabel = il.DefineLabel();

            /////////////////////////////
            // 1. GET CURRENT FIELD INDEX
            // switch (ProtoDynamicBase.GetColumnIndex(P_0))
            /////////////////////////////
            var getNextColumnIndexLabel = AddLoadCurrentFieldIndex(il, switchFieldIndexLabel);

            var labelFarm = new Dictionary<Int32, Label>();
            var max = 0;

            //////////////////////////////////////////////////////////////////
            // 3. LOGIC PER FIELD VIA SWIwTCH
            //////////////////////////////////////////////////////////////////
            foreach (var currentProp in fieldArr)
            {
                state.CurrentField = currentProp;

                //////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////
                var switchLabel = AddScanProperty(state, whileLabel,
                    canSetValuesInline, exampleObject);
                //////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////

                labelFarm[currentProp.Index] = switchLabel;
                max = Math.Max(max, currentProp.Index);
            }
            //////////////////////////////////////////////////////////////////

            // build the jump based on observations but fill in gaps
            var jt = new Label[max + 1];
            for (var c = 0; c <= max; c++)
                if (labelFarm.TryGetValue(c, out var baa))
                    jt[c] = baa;
                else
                    jt[c] = afterPropertyLabel;

            ////////////////////////////
            // 2. SWITCH ON FIELD INDEX
            ////////////////////////////
            il.MarkLabel(switchFieldIndexLabel);
            il.Emit(OpCodes.Switch, jt);


            //////////////////////////////////////////
            // 4. WHILE (STREAM.INDEX < STREAM.LENGTH)
            //////////////////////////////////////////
            il.MarkLabel(whileLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, _getStreamPosition);

            il.Emit(OpCodes.Ldloc, streamLength);

            il.Emit(OpCodes.Blt, getNextColumnIndexLabel);
            il.Emit(OpCodes.Br, afterPropertyLabel);
        }

        /// <summary>
        ///     1. Creates a method to deserialize from a stream
        /// </summary>
        private void AddScanMethod(Type parentType,
                                   TypeBuilder bldr,
                                   Type genericParent,
                                   IEnumerable<IProtoFieldAccessor> fields,
                                   Object? exampleObject,
                                   Boolean canSetValuesInline,
                                   MethodBase retBldr,
                                   IDictionary<Type, ProxiedInstanceField> proxies)
        {
            var fieldArr = fields.ToArray();

            var method = PrepareScanMethod(bldr, genericParent, parentType,
                out var abstractMethod, out var il);

            ///////////////
            // VARIABLES
            //////////////
            var streamLength = il.DeclareLocal(typeof(Int64));
            var lastByte = il.DeclareLocal(typeof(Int32));

            var endLabel = il.DefineLabel();

            EnsureThreadLocalByteArray(il, fieldArr);

            //////////////////////////////
            // INSTANTIATE RETURN VALUE
            /////////////////////////////
            if (canSetValuesInline)
                il.Emit(OpCodes.Ldarg_0);

            var returnValue = InstantiateObjectToDefault(il, parentType,
                retBldr, fieldArr,
                exampleObject);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, _getStreamPosition);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, streamLength);
            /////////////////////////////

            Action<ILGenerator>? loadCurrentValue = null;
            if (canSetValuesInline)
                loadCurrentValue = ilg => ilg.Emit(OpCodes.Ldloc, returnValue);

            if (fieldArr.Length == 0)
                throw new InvalidOperationException();

            var first = fieldArr[0];

            var state = new ProtoScanState(il, fieldArr, first, parentType, loadCurrentValue,
                lastByte, this, _readBytesField,
                _types, _instantiator, proxies, this);

            /////////////////////////////
            AddPropertiesToScanMethod(state, endLabel,
                canSetValuesInline, streamLength, exampleObject);
            /////////////////////////////

            il.MarkLabel(endLabel);

            if (!canSetValuesInline)
                InstantiateFromLocals(state, retBldr);
            else
            {
                SetPropertiesFromLocals(state, returnValue);
                il.Emit(OpCodes.Ldloc, returnValue);
            }

            il.Emit(OpCodes.Ret);
            bldr.DefineMethodOverride(method, abstractMethod);
        }

        /// <summary>
        ///     3.
        /// </summary>
        private Label AddScanProperty(IProtoScanState s,
                                      Label afterPropertyLabel,
                                      Boolean canSetValueInline,
                                      Object? exampleObject)
        {
            // for collections that we can directly add values to as they were initialized 
            // by the constructor
            var isValuePreInitialized = exampleObject != null &&
                                        _types.IsCollection(s.CurrentField.Type) &&
                                        _objects.GetPropertyValue(exampleObject,
                                            s.CurrentField.Name, PropertyNameFormat.Default) != null;

            var il = s.IL;
            var currentProp = s.CurrentField;

            var caseEntryForThisProperty = s.IL.DefineLabel();
            s.IL.MarkLabel(caseEntryForThisProperty);

            //1. load value's destination onto the stack
            var initValueTarget = s.GetFieldSetInit(currentProp, canSetValueInline);
            initValueTarget(currentProp, s);

            // 2.   load value itself onto the stack unless it's a packed array where the property is an ICollection<IntXX>
            //      in which case it is loaded and set at the same time
            //////////////////////////////////////////////////////////////////
            ScanValueToStack(s, il, currentProp.Type, currentProp.TypeCode,
                currentProp.WireType, currentProp.FieldAction, isValuePreInitialized);
            //////////////////////////////////////////////////////////////////

            //3. assign value to destination
            var storeValue = s.GetFieldSetCompletion(currentProp, canSetValueInline, isValuePreInitialized);
            storeValue(currentProp, s);

            il.Emit(OpCodes.Br, afterPropertyLabel);

            return caseEntryForThisProperty;
        }

        private void EnsureThreadLocalByteArray(ILGenerator il,
                                                IProtoFieldAccessor[] fields)
        {
            var dothProceed = false;

            foreach (var field in fields)
            {
                var germane = _types.GetGermaneType(field.Type);

                if (!germane.IsIn(typeof(String), typeof(Double), typeof(Single)))
                    continue;

                dothProceed = true;
                break;
            }

            if (!dothProceed)
                return;

            var instantiated = il.DefineLabel();

            //local threadstatic byte[] buffer
            il.Emit(OpCodes.Ldsfld, _readBytesField);
            il.Emit(OpCodes.Brtrue_S, instantiated);

            il.Emit(OpCodes.Ldc_I4, 256);
            il.Emit(OpCodes.Newarr, typeof(Byte));

            il.Emit(OpCodes.Stsfld, _readBytesField);

            il.MarkLabel(instantiated);
        }

        private static void InstantiateFromLocals(ProtoScanState s,
                                                  MethodBase buildReturnValue)
        {
            if (!(buildReturnValue is ConstructorInfo ctor))
                throw new InvalidOperationException();

            var ctorParams = ctor.GetParameters();

            for (var c = 0; c < ctorParams.Length; c++)
            {
                var current = ctorParams[c];

                var local = s.GetLocalForParameter(current);

                s.IL.Emit(OpCodes.Ldloc, local);
            }

            s.IL.Emit(OpCodes.Newobj, ctor);
        }


        private LocalBuilder InstantiateObjectToDefault(ILGenerator il,
                                                        Type type,
                                                        MethodBase? buildDefault,
                                                        IEnumerable<IProtoFieldAccessor> fields,
                                                        Object? exampleObject)
        {
            var returnValue = il.DeclareLocal(type);

            switch (buildDefault)
            {
                case ConstructorInfo ctor when ctor.GetParameters().Length == 0:
                    il.Emit(OpCodes.Newobj, ctor);
                    break;

                case ConstructorInfo _: //can't instantiate yet
                    return returnValue;

                case MethodInfo bdor:
                    il.Emit(OpCodes.Callvirt, bdor);
                    break;

                case null:
                    return returnValue;

                default:
                    throw new InvalidOperationException();
            }

            il.Emit(OpCodes.Stloc, returnValue);

            if (exampleObject == null)
                return returnValue;

            foreach (var field in fields)
            {
                if (!field.IsRepeatedField)
                    continue;

                if (field.FieldAction == FieldAction.ChildObjectArray)
                    continue; //don't try to instantiate arrays

                if (type.IsAssignableFrom(exampleObject.GetType()))
                {
                    var getter = type.GetterOrDie(field.Name, out var propInfo);
                    var exampleValue = getter.Invoke(exampleObject, null);

                    if (exampleValue != null || !propInfo.CanWrite)
                        continue;
                }

                var setter = type.SetterOrDie(field.Name);

                if (_instantiator.TryGetDefaultConstructor(field.Type, out var fieldCtor))
                {
                    il.Emit(OpCodes.Ldloc, returnValue);
                    il.Emit(OpCodes.Newobj, fieldCtor);
                    il.Emit(OpCodes.Callvirt, setter);
                }
            }

            return returnValue;
        }

        private static MethodInfo PrepareScanMethod(TypeBuilder bldr,
                                                    Type genericParent,
                                                    Type parentType,
                                                    out MethodInfo abstractMethod,
                                                    out ILGenerator il)
        {
            var methodArgs = new[] {typeof(Stream), typeof(Int64)};

            abstractMethod = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.Scan), Const.PublicInstance, methodArgs);

            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Scan),
                MethodOverride, parentType, methodArgs);

            il = method.GetILGenerator();

            return method;
        }

        private static void ScanChildObject(Type type,
                                            IProtoScanState s)
        {
            var il = s.IL;

            var proxy = s.GetProxy(type);
            var proxyLocal = proxy.ProxyField;
            var proxyType = proxyLocal.FieldType;

            ////////////////////////////////////////////
            // PROXY->SCAN(CURRENT)
            ////////////////////////////////////////////
            var scanMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Scan),
                typeof(Stream), typeof(Int64));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, proxyLocal); //protoProxy
            il.Emit(OpCodes.Ldarg_1); //arg1 = input stream!

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt64);

            il.Emit(OpCodes.Call, scanMethod);
        }

        /// <summary>
        ///     Can be an actual field or an instance of a collection field.
        /// Leaves the value on the stack
        /// </summary>
        private void ScanValueToStack(IProtoScanState s,
                                      ILGenerator il,
                                      Type fieldType,
                                      TypeCode typeCode,
                                      ProtoWireTypes wireType,
                                      FieldAction fieldAction,
                                      Boolean isValuePreInitialized)
        {
            switch (fieldAction)
            {
                case FieldAction.VarInt:
                case FieldAction.Primitive:
                    ScanAsVarInt(il, typeCode, wireType);
                    return;

                case FieldAction.String:
                    s.LoadNextString();
                    return;

                case FieldAction.ByteArray:
                    ScanByteArray(il, s.LastByteLocal);
                    return;

                case FieldAction.PackedArray:
                    ScanAsPackedArray(il, fieldType, isValuePreInitialized);
                    return;

                case FieldAction.ChildObject:
                    ScanChildObject(fieldType, s);
                    return;

                case FieldAction.ChildObjectCollection:
                case FieldAction.ChildPrimitiveCollection:
                    ScanCollection(fieldType, s);
                    return;

                case FieldAction.ChildObjectArray:
                case FieldAction.ChildPrimitiveArray:
                    ScanCollection(fieldType, s);
                    return;

                case FieldAction.Dictionary:
                    ScanCollection(fieldType, s);
                    return;

                case FieldAction.DateTime:
                    ScanAsVarInt(il, TypeCode.Int64, ProtoWireTypes.Varint);
                    il.Emit(OpCodes.Call, _dateFromFileTime);
                    break;

                case FieldAction.HasSpecialProperty:
                case FieldAction.FallbackSerializable:
                
                    if (TryGetSpecialProperty(s.CurrentField.Type, out var pInfo))
                    {
                        var ctor = s.CurrentField.Type.GetConstructorOrDie(new[] 
                            { pInfo.PropertyType });

                        var fa = GetProtoFieldAction(pInfo.PropertyType);
                        var propType = pInfo.PropertyType;
                        var wt = ProtoBufSerializer.GetWireType(propType);
                        ScanValueToStack(s, il, propType, Type.GetTypeCode(propType),
                            wt, fa, isValuePreInitialized);
                        il.Emit(OpCodes.Newobj, ctor);
                    }
                    else throw new NotImplementedException();
                    break;

                
                case FieldAction.NullableValueType:
                case FieldAction.Enum:
                
                
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SetPropertiesFromLocals(ProtoScanState state,
                                                    LocalBuilder returnValue)
        {
            var il = state.IL;

            foreach (var kvp in state.LocalFieldValues)
            {
                il.Emit(OpCodes.Ldloc, returnValue);

                il.Emit(OpCodes.Ldloc, kvp.Value);

                var converter = kvp.Value.LocalType!.GetMethod(nameof(List<Object>.ToArray));
                if (converter != null)
                    il.Emit(OpCodes.Callvirt, converter);

                var setter = kvp.Key.SetMethod ?? throw new InvalidOperationException(kvp.Key.Name);

                il.Emit(OpCodes.Callvirt, setter);
            }
        }
    }
}

#endif
