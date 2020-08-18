using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Extensions;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedTypeParameter
    // ReSharper disable once UnusedType.Global
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        /// <summary>
        /// 1
        /// </summary>
        private void AddScanMethod(
            Type parentType, 
            TypeBuilder bldr,
            Type genericParent, 
            IEnumerable<IProtoFieldAccessor> fields,
            Object? exampleObject,
            Boolean canSetValuesInline, 
            MethodBase buildReturnValue,
            IDictionary<Type, FieldBuilder> proxies)
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
                buildReturnValue, fieldArr,
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
                _types, _instantiator, proxies);

            /////////////////////////////
            AddPropertiesToScanMethod(state, endLabel, 
                canSetValuesInline, streamLength, exampleObject);
            /////////////////////////////
            
            il.MarkLabel(endLabel);

            if (!canSetValuesInline)
                InstantiateFromLocals(state, buildReturnValue);
            else
            {
                SetPropertiesFromLocals(state, returnValue);
                il.Emit(OpCodes.Ldloc, returnValue);
            }

            il.Emit(OpCodes.Ret);
            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private static MethodInfo PrepareScanMethod(
            TypeBuilder bldr, 
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

        private void EnsureThreadLocalByteArray(ILGenerator il, IProtoFieldAccessor[] fields)
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

        /// <summary>
        /// 2.
        /// </summary>
        private void AddPropertiesToScanMethod(
            ProtoScanState state,
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
            {
                if (labelFarm.TryGetValue(c, out var baa))
                    jt[c] = baa;
                else
                    jt[c] = afterPropertyLabel;
            }

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

            il.Emit(OpCodes.Blt,getNextColumnIndexLabel);
            il.Emit(OpCodes.Br, afterPropertyLabel);
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

        private static void SetPropertiesFromLocals(ProtoScanState state, LocalBuilder returnValue)
        {
            var il = state.IL;

            foreach (var kvp in state.LocalFieldValues)
            {
                il.Emit(OpCodes.Ldloc, returnValue);
                
                il.Emit(OpCodes.Ldloc, kvp.Value);

                var converter = kvp.Value.LocalType.GetMethod(nameof(List<Object>.ToArray));
                if (converter != null)
                    il.Emit(OpCodes.Callvirt, converter);

                var setter = kvp.Key.SetMethod ?? throw new InvalidOperationException(kvp.Key.Name);

                il.Emit(OpCodes.Callvirt, setter);
            }
        }


        private LocalBuilder InstantiateObjectToDefault(ILGenerator il, Type type,
            MethodBase? buildDefault, IEnumerable<IProtoFieldAccessor> fields,
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

                if (field.FieldAction == ProtoFieldAction.ChildObjectArray)
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

        /// <summary>
        /// 3.
        /// </summary>
        private Label AddScanProperty(ProtoScanState s,
                                      Label afterPropertyLabel,
                                      Boolean canSetValueInline,
                                      Object? exampleObject)
        {
            // for collections that we can directly add values to as they were initialized 
            // by the constructor
            var isValuePreInitialized = exampleObject != null &&
                                        _types.IsCollection(s.CurrentField.Type) && 
                                        _objects.GetPropertyValue(exampleObject,
                                            s.CurrentField.Name) != null;

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

        /// <summary>
        /// Can be an actual field or an instance of a collection field.  Leaves the value on the stack
        /// </summary>
        private void ScanValueToStack(
            IValueExtractor s,
            ILGenerator il,
            Type fieldType,
            TypeCode typeCode,
            ProtoWireTypes wireType,
            ProtoFieldAction fieldAction,
            Boolean isValuePreInitialized)
        {
            switch (fieldAction)
            {
                case ProtoFieldAction.VarInt:
                case ProtoFieldAction.Primitive:
                    ScanAsVarInt(il, typeCode, wireType);
                    return;

                case ProtoFieldAction.String:
                    s.LoadNextString();
                    return;

                case ProtoFieldAction.ByteArray:
                    ScanByteArray(il, s.LastByteLocal);
                    return;

                case ProtoFieldAction.PackedArray:
                    ScanAsPackedArray(il, fieldType, isValuePreInitialized);
                    return;

                case ProtoFieldAction.ChildObject:
                    ScanChildObject(fieldType, s);
                    return;

                case ProtoFieldAction.ChildObjectCollection:
                case ProtoFieldAction.ChildPrimitiveCollection:
                    ScanCollection(fieldType, s);
                    return;

                case ProtoFieldAction.ChildObjectArray:
                case ProtoFieldAction.ChildPrimitiveArray:
                    ScanCollection(fieldType, s);
                    return;

                case ProtoFieldAction.Dictionary:
                    ScanCollection(fieldType, s);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ScanChildObject(Type type, IValueExtractor s)
        {
            var il = s.IL;

            var proxyLocal = s.GetProxy(type);
            var proxyType = proxyLocal.FieldType;

            ////////////////////////////////////////////
            // PROXY->SCAN(CURRENT)
            ////////////////////////////////////////////
            var scanMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Scan),
                typeof(Stream), typeof(Int64));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, proxyLocal); //protoProxy
            il.Emit(OpCodes.Ldarg_1);  //arg1 = input stream!
            
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt64);

            il.Emit(OpCodes.Call, scanMethod);
        }


        /// <summary>
        /// Puts the next field index on the stack.  Jumps to <param name="goNextLabel" />
        /// </summary>
        private Label AddLoadCurrentFieldIndex(ILGenerator il, Label goNextLabel)
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
    }
}

