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
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        /// <summary>
        /// 1
        /// </summary>
        private void AddScanMethod(Type parentType, TypeBuilder bldr,
            Type genericParent, IEnumerable<IProtoFieldAccessor> fields,
            Object? exampleObject,
            IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
            Boolean canSetValuesInline, MethodBase buildReturnValue,
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

            EnsureThreadLocalByteArray(il);

            //////////////////////////////
            // INSTANTIATE RETURN VALUE
            /////////////////////////////
            var arrayCounters = new ProtoArrayInfo(_getStreamPosition, _types, _getPositiveInt32);

            if (canSetValuesInline)
                il.Emit(OpCodes.Ldarg_0);

            var returnValue = InstantiateObjectToDefault(il, parentType, 
                buildReturnValue, fieldArr, arrayCounters, exampleObject);

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
                lastByte, exampleObject, arrayCounters, childProxies, this, _readBytesField,
                _types, _instantiator, proxies);

            /////////////////////////////
            AddPropertiesToScanMethod(state, endLabel, canSetValuesInline, streamLength);
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

        private void EnsureThreadLocalByteArray(ILGenerator il)
        {
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
            LocalBuilder streamLength)
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
                var switchLabel = AddScanProperty(state, whileLabel, canSetValuesInline);
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

        private void InstantiateFromLocals(ProtoScanState s,
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

        private void SetPropertiesFromLocals(ProtoScanState state, LocalBuilder returnValue)
        {
            var il = state.IL;

            foreach (var kvp in state.LocalFieldValues)
            {
                il.Emit(OpCodes.Ldloc, returnValue);
                
                il.Emit(OpCodes.Ldloc, kvp.Value);

                var converter = kvp.Value.LocalType.GetMethod(nameof(List<Object>.ToArray));
                if (converter != null)
                    il.Emit(OpCodes.Callvirt, converter);


                il.Emit(OpCodes.Callvirt, kvp.Key.SetMethod);
            }
        }


        private LocalBuilder InstantiateObjectToDefault(ILGenerator il, Type type,
            MethodBase? buildDefault, IEnumerable<IProtoFieldAccessor> fields, 
            ProtoArrayInfo arrayBuilders, Object? exampleObject)
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

                //if (field.Type.IsArray)
                if (field.FieldAction == ProtoFieldAction.ChildObjectArray)
                {
                    arrayBuilders.Add(field, il);
                    continue;
                }

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
        private Label AddScanProperty(ProtoScanState s, Label afterPropertyLabel,
            Boolean canSetValueInline)
        {
            var il = s.IL;
            var currentProp = s.CurrentField;

            var initValueTarget = s.GetFieldSetInit(currentProp, canSetValueInline);
            var storeValue = s.GetFieldSetCompletion(currentProp, canSetValueInline);


            var caseEntryForThisProperty = s.IL.DefineLabel();
            s.IL.MarkLabel(caseEntryForThisProperty);


            initValueTarget(currentProp, s);

            //////////////////////////////////////////////////////////////////
            PopulateScanField(s, il, currentProp.Type, currentProp.TypeCode, 
                currentProp.WireType, currentProp.FieldAction);
            //////////////////////////////////////////////////////////////////

            storeValue(currentProp, s);
          

            il.Emit(OpCodes.Br, afterPropertyLabel);

            return caseEntryForThisProperty;
        }

        /// <summary>
        /// 3.
        /// </summary>
        private Label AddScanProperty2(ProtoScanState s, Label afterPropertyLabel,
            Boolean canSetValueInline)
        {
            var il = s.IL;
            var currentProp = s.CurrentField;

            var initValueTarget = s.GetFieldSetInit(currentProp, canSetValueInline);
            var storeValue = s.GetFieldSetCompletion(currentProp, canSetValueInline);


            var canSetFieldInline = canSetValueInline &&
                                    (!currentProp.Type.IsArray ||
                                     currentProp.FieldAction == ProtoFieldAction.PackedArray);

            var setter = canSetValueInline
                ? s.ParentType.SetterOrDie(currentProp.Name)
                : null;

            var holdForSet = canSetFieldInline
                ? null
                : s.GetLocalForField(currentProp);

            var caseEntryForThisProperty = s.IL.DefineLabel();
            s.IL.MarkLabel(caseEntryForThisProperty);

            Action<ILGenerator>? setCurrentValue = null;
            if (canSetValueInline && setter != null)
                setCurrentValue  = ill => ill.Emit(OpCodes.Callvirt, setter);

            //////////////////////////////////////////////////////////////////
            PopulateScanField(s, setCurrentValue, ref holdForSet!,
                initValueTarget, storeValue);
            //////////////////////////////////////////////////////////////////

            if (canSetValueInline && holdForSet != null && 
                s.LoadCurrentValueOntoStack != null && setter != null)
            {
                s.LoadCurrentValueOntoStack(il);
                il.Emit(OpCodes.Ldloc, holdForSet);
                il.Emit(OpCodes.Callvirt, setter);
            }

            il.Emit(OpCodes.Br, afterPropertyLabel);

            return caseEntryForThisProperty;
        }

        private void PopulateScanField(ProtoScanState s, 
            Action<ILGenerator>? setCurrentValue,
                ref LocalBuilder holdForSet,
            Action<IProtoFieldAccessor, ProtoScanState> initValueTarget,
            Action<IProtoFieldAccessor, ProtoScanState> storeValue)
        {
            var currentProp = s.CurrentField ?? throw new NullReferenceException(nameof(s.CurrentField));

            var il = s.IL;
            var typeCode = currentProp.TypeCode;
            var wireType = currentProp.WireType;
            var fieldType = currentProp.Type;

            switch (currentProp.FieldAction)
            {
                case ProtoFieldAction.VarInt:
                case ProtoFieldAction.Primitive:
                    
                    //TryScanAsVarInt(s, setCurrentValue, ref holdForSet);
                    ScanAsVarInt(il,typeCode, wireType);

                    return;

                case ProtoFieldAction.String:
                    
                    //ScanString(s, setCurrentValue, ref holdForSet);
                    //ScanString(s);
                    s.LoadNextString();

                    return;

                case ProtoFieldAction.ByteArray:
                    
                    //ScanByteArray(s, ref holdForSet);
                    ScanByteArray(il, s.LastByteLocal);
                    
                    return;

                case ProtoFieldAction.PackedArray:
                    //TryScanAsPackedArray(s, setCurrentValue, ref holdForSet);
                    ScanAsPackedArray(il, fieldType);
                    return;

                case ProtoFieldAction.ChildObject:
                    
                    //ScanChildObject(s, setCurrentValue, ref holdForSet);
                    ScanChildObject(fieldType, s);

                    return;

                case ProtoFieldAction.ChildObjectCollection:
                case ProtoFieldAction.ChildPrimitiveCollection:

                    //ScanCollection(fieldType, s);

                    TryScanAsNestedCollection(s,
                        LoadCurrentFieldAsCollection,
                        AddSingleValue);

                    return;

                case ProtoFieldAction.ChildObjectArray:
                case ProtoFieldAction.ChildPrimitiveArray:
                    TryScanAsNestedCollection(s, 
                        LoadLocalListForArray,
                        AddSingleValue);
                    break;

                case ProtoFieldAction.Dictionary:
                    TryScanAsNestedCollection(s,
                        LoadCurrentFieldAsCollection,
                        AddKeyValuePair);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

         private void PopulateScanField(
             IValueExtractor s,
             ILGenerator il, 
             Type fieldType,
             TypeCode typeCode,
             ProtoWireTypes wireType,
             ProtoFieldAction fieldAction)
        {
            switch (fieldAction)
            {
                case ProtoFieldAction.VarInt:
                case ProtoFieldAction.Primitive:
                    
                    //TryScanAsVarInt(s, setCurrentValue, ref holdForSet);
                    ScanAsVarInt(il,typeCode, wireType);

                    return;

                case ProtoFieldAction.String:
                    
                    //ScanString(s, setCurrentValue, ref holdForSet);
                    //ScanString(s);
                    s.LoadNextString();

                    return;

                case ProtoFieldAction.ByteArray:
                    
                    //ScanByteArray(s, ref holdForSet);
                    ScanByteArray(il, s.LastByteLocal);
                    
                    return;

                case ProtoFieldAction.PackedArray:
                    //TryScanAsPackedArray(s, setCurrentValue, ref holdForSet);
                    ScanAsPackedArray(il, fieldType);
                    return;

                case ProtoFieldAction.ChildObject:
                    
                    //ScanChildObject(s, setCurrentValue, ref holdForSet);
                    ScanChildObject(fieldType, s);

                    return;

                case ProtoFieldAction.ChildObjectCollection:
                case ProtoFieldAction.ChildPrimitiveCollection:
                    
                    ScanCollection(fieldType, s);

                    //TryScanAsNestedCollection(s, 
                    //    LoadCurrentFieldAsCollection,
                    //    AddSingleValue);

                    return;

                case ProtoFieldAction.ChildObjectArray:
                case ProtoFieldAction.ChildPrimitiveArray:
                    
                    ScanCollection(fieldType, s);

                    //TryScanAsNestedCollection(s, 
                    //    LoadLocalListForArray,
                    //    AddSingleValue);
                    break;

                case ProtoFieldAction.Dictionary:
                    ScanCollection(fieldType, s);
                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void LoadCurrentFieldAsCollection(ProtoScanState s, IProtoFieldAccessor pv)
        {
            s.LoadCurrentFieldValueToStack();
        }

        /// <summary>
        /// Leaves a reference on the stack!
        /// </summary>
        
        private void ScanChildObject(
            ProtoScanState s, 
            Action<ILGenerator>? setCurrentValue,
            ref LocalBuilder holdForSet)
        {
            var il = s.IL;

            var pv = s.CurrentField ?? throw new NullReferenceException(nameof(s.CurrentField));

            var proxyLocal = s.ChildProxies[pv];
            var proxyType = proxyLocal.FieldType;

            
            ////////////////////////////////////////////
            // PROXY->SCAN(CURRENT)
            ////////////////////////////////////////////
            var scanMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Scan),
                typeof(Stream), typeof(Int64));


            if (setCurrentValue != null)
                s.LoadCurrentValueOntoStack(il);
            else
                holdForSet ??= il.DeclareLocal(pv.Type);


            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, proxyLocal); //protoProxy
            il.Emit(OpCodes.Ldarg_1);  //arg1 = input stream!
            
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt64);

            il.Emit(OpCodes.Call, scanMethod);


            if (setCurrentValue != null)
                setCurrentValue(il);
            else
                il.Emit(OpCodes.Stloc, holdForSet);
        }

        private void ScanChildObject(
            Type type,
            IValueExtractor s)
        {
            var il = s.IL;

            //var pv = s.CurrentField ?? throw new NullReferenceException(nameof(s.CurrentField));

            //var proxyLocal = s.ChildProxies[pv];
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

        private void ExtractInt64FromArg1Stream(ILGenerator il,
            ref LocalBuilder holdForSet)
        {
            holdForSet = holdForSet ?? il.DeclareLocal(typeof(Int64));
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getInt64);
            il.Emit(OpCodes.Stloc, holdForSet);
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

