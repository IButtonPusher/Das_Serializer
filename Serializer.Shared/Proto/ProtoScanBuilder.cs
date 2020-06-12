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
        private void AddScanMethod(Type parentType, TypeBuilder bldr,
            Type genericParent, IEnumerable<IProtoFieldAccessor> fields, 
            Object? exampleObject,
            IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
            Boolean canSetValuesInline, MethodBase buildReturnValue)
        {
            var fieldArr = fields.ToArray();


            var method = PrepareScanMethod(bldr, genericParent, parentType, 
                out var abstractMethod, out var il);


            ///////////////
            // VARIABLES
            //////////////
            var streamLength = il.DeclareLocal(typeof(Int64));
            var lastByte = il.DeclareLocal(typeof(Int32));

            //var fieldByteArray = il.DeclareLocal(typeof(Byte[]));
            var endLabel = il.DefineLabel();
           
            EnsureThreadLocalByteArray(il);

            //////////////////////////////
            // INSTANTIATE RETURN VALUE
            /////////////////////////////
            var arrayCounters = new ProtoArrayInfo(_getStreamPosition, _types, _getPositiveInt32);

            if (canSetValuesInline)
                il.Emit(OpCodes.Ldarg_0);

            var returnValue = InstantiateObjectToDefault(il, parentType, buildReturnValue, fieldArr,
                arrayCounters, exampleObject);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, _getStreamPosition);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, streamLength);
            /////////////////////////////

            Action<ILGenerator> loadCurrentValue = null;
            if (canSetValuesInline)
                loadCurrentValue = ilg => ilg.Emit(OpCodes.Ldloc, returnValue);


            var state = new ProtoScanState(il, fieldArr, null, parentType, loadCurrentValue,
                lastByte, exampleObject, arrayCounters, childProxies, this, _types, _readBytesField);


            /////////////////////////////
            AddPropertiesToScanMethod(state, endLabel, canSetValuesInline, streamLength);
                
                //fieldArr, parentType, il,
                //loadCurrentValue, endLabel,
                //lastByte, streamLength,
                //arrayCounters, exampleObject, childProxies, canSetValuesInline);
            /////////////////////////////

            il.MarkLabel(endLabel);

            if (!canSetValuesInline)
                InstantiateFromLocals(state, buildReturnValue);
            else
                il.Emit(OpCodes.Ldloc, returnValue);

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

        //private void AddPropertiesToScanMethod(IEnumerable<IProtoFieldAccessor> fields,
        //    Type parentType, ILGenerator il, 
        //    Action<ILGenerator>? loadObject,
        //    Label afterPropertyLabel, 
        //    LocalBuilder lastByte, LocalBuilder streamLength, 
        //    ProtoArrayInfo arrayCounters, Object? exampleObject,
        //    IDictionary<IProtoFieldAccessor, FieldBuilder> childProxies,
        //    Boolean canSetValuesInline)
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
            // 3. LOGIC PER FIELD VIA SWITCH
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

                //var lode = local.LocalType.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc;

                s.IL.Emit(OpCodes.Ldloc, local);
            }

            s.IL.Emit(OpCodes.Newobj, ctor);
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

        private Label AddScanProperty(ProtoScanState s, Label afterPropertyLabel,
            Boolean canSetValueInline)
        {
            var il = s.IL;
            var currentProp = s.CurrentField ?? throw new NullReferenceException(nameof(s.CurrentField));

            var setter = canSetValueInline
                ? s.ParentType.SetterOrDie(currentProp.Name)
                : null;

            var holdForSet = canSetValueInline
                ? null
                : s.GetLocalForField(currentProp);

            var caseEntryForThisProperty = s.IL.DefineLabel();
            s.IL.MarkLabel(caseEntryForThisProperty);

            //s.SetCurrentValue = ill => ill.Emit(OpCodes.Callvirt, setter);

            Action<ILGenerator>? setCurrentValue = null;
            if (canSetValueInline && setter != null)
                setCurrentValue  = ill => ill.Emit(OpCodes.Callvirt, setter);


            //////////////////////////////////////////////////////////////////
            PopulateScanField(s, setCurrentValue, ref holdForSet!);
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
                ref LocalBuilder holdForSet)
        {
            var currentProp = s.CurrentField ?? throw new NullReferenceException(nameof(s.CurrentField));


            var wireType = currentProp.WireType;
            var il = s.IL;


            switch (currentProp.FieldAction)
            {
                case ProtoFieldAction.VarInt:
                case ProtoFieldAction.Primitive:
                    TryScanAsVarInt(s, setCurrentValue, ref holdForSet);
                    return;

                case ProtoFieldAction.String:
                    ScanString(s, setCurrentValue, ref holdForSet);
                    return;

                case ProtoFieldAction.ByteArray:
                    ScanByteArray(s, ref holdForSet);
                    return;

                case ProtoFieldAction.PackedArray:
                    TryScanAsPackedArray(s, setCurrentValue, ref holdForSet);
                    return;

                case ProtoFieldAction.ChildObject:
                    ScanChildObject(s, setCurrentValue, ref holdForSet);
                    return;

                case ProtoFieldAction.ChildObjectCollection:
                case ProtoFieldAction.ChildPrimitiveCollection:
                    TryScanAsNestedCollection(s, AddSingleValue);
                    return;

                case ProtoFieldAction.ChildObjectArray:
                case ProtoFieldAction.ChildPrimitiveArray:
                    throw new NotImplementedException();
                

                case ProtoFieldAction.Dictionary:
                    TryScanAsNestedCollection(s, AddKeyValuePair);
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            switch (wireType)
            {
                case Const.VarInt:
                case ProtoWireTypes.Int64: //64-bit zb double
                case ProtoWireTypes.Int32:
                    TryScanAsVarInt(s, setCurrentValue, ref holdForSet!);
                    break;

                case ProtoWireTypes.LengthDelimited when currentProp.Type.IsPrimitive:
                    throw new NotImplementedException();


                case ProtoWireTypes.LengthDelimited:

                    //string, byte[]
                    switch (currentProp.TypeCode)
                    {
                        ///////////
                        // STRING
                        ///////////
                        case TypeCode.String:
                        {
                            il.Emit(OpCodes.Ldarg_1);
                            holdForSet = holdForSet ?? il.DeclareLocal(typeof(String));

                            //Get length of string's bytes
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, _getPositiveInt32);
                            il.Emit(OpCodes.Stloc, s.LastByteLocal);
                            
                            //read bytes into buffer field
                            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
                            il.Emit(OpCodes.Ldsfld, _readBytesField);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
                            il.Emit(OpCodes.Callvirt, _readStreamBytes);
                            il.Emit(OpCodes.Pop);

                            //base.Utf8.GetString(fieldByteArray);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, _utf8);
                            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
                            il.Emit(OpCodes.Ldsfld, _readBytesField);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
                            il.Emit(OpCodes.Call, _bytesToString);
                            il.Emit(OpCodes.Stloc, holdForSet);
                        }
                            break;

                        case TypeCode.Object:

                            ////////////////
                            // BYTE [ARRAY]
                            ////////////////
                            if (currentProp.Type == Const.ByteArrayType)
                            {
                                il.Emit(OpCodes.Ldarg_1);
                                holdForSet ??= il.DeclareLocal(typeof(Byte[]));

                                //Get length of the array
                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Call, _getPositiveInt32);

                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Stloc, s.LastByteLocal);
                                il.Emit(OpCodes.Newarr, typeof(Byte));
                                il.Emit(OpCodes.Stloc, holdForSet);

                                //read bytes into buffer field
                                il.Emit(OpCodes.Ldloc, holdForSet);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ldloc, s.LastByteLocal);
                                il.Emit(OpCodes.Callvirt, _readStreamBytes);

                                il.Emit(OpCodes.Pop);

                            }

                            else if (TryScanAsPackedArray(s, setCurrentValue, ref holdForSet))
                                //il, currentProp, lastByte, 
                                //s.LoadCurrentValueOntoStack,
                                //setValue, ref holdForSet, exampleObject, parentType))
                                return;

                           
                            ///////////////////////
                            // NESTED PROTOCONTRACT
                            ///////////////////////
                            else if (!currentProp.IsRepeatedField)
                            {
                                var willAddToExisting = TryLoadTargetReference(s, setCurrentValue);
                                    //il, currentProp, loadObject, setValue,
                                    //parentType);

                                var childFields = GetProtoFields(currentProp.Type);

                                if (willAddToExisting)
                                {
                                    holdForSet ??= il.DeclareLocal(currentProp.Type);
                                    il.Emit(OpCodes.Stloc, holdForSet);
                                }
                                else
                                {
                                    //instantiate an object of the property 
                                    _types.TryGetEmptyConstructor(currentProp.Type, out var ctor);

                                    
                                    //var ctor = currentProp.Type.GetConstructor(Type.EmptyTypes)
                                    //           ?? throw new MissingMethodException();

                                    

                                    var subArrayCounters = new ProtoArrayInfo(_getStreamPosition, _types,
                                        _getPositiveInt32);

                                    holdForSet = InstantiateObjectToDefault(il, currentProp.Type, ctor,
                                        childFields, subArrayCounters, s.ExampleObject);
                                }

                                var endOfPropLabel = il.DefineLabel();
                                var propLength = il.DeclareLocal(typeof(Int64));

                                //calc where this property ends in the stream
                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Callvirt, _getStreamPosition);
                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Call, _getPositiveInt64);
                                il.Emit(OpCodes.Add);
                                il.Emit(OpCodes.Stloc, propLength);
                                

                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Callvirt, _getStreamPosition);
                                il.Emit(OpCodes.Ldloc, propLength);

                                il.Emit(OpCodes.Bge, endOfPropLabel);

                                //var holdForSetCopy = holdForSet;

                                throw new NotImplementedException();

                                //AddPropertiesToScanMethod(childFields, currentProp.Type, il,
                                //    ilg => ilg.Emit(OpCodes.Ldloc, holdForSetCopy), endOfPropLabel,
                                //    fieldByteArray, lastByte, propLength, arrayCounters,
                                //    exampleObject);

                                //il.MarkLabel(endOfPropLabel);
                            }

                            //////////////
                            // COLLECTIONS
                            //////////////
                            else
                            {
                                if (currentProp.Type.IsArray)
                                {
                                    ///////////
                                    // ARRAYS
                                    ///////////
                                    s.ArrayCounters.Increment(currentProp, il);
                                }

                                //////////////
                                // DICTIONARY
                                //////////////
                                else if (TryScanAsDictionary(s, setCurrentValue))
                                    //il, currentProp, fieldByteArray,
                                    //lastByte, loadObject, parentType, arrayCounters, exampleObject))
                                    break;
                                else
                                {
                                    ////////////////////
                                    // OTHER COLLECTIONS
                                    ////////////////////

                                    TryScanAsNestedCollection(s, AddSingleValue);
                                    //il, currentProp, 
                                    //fieldByteArray, lastByte, ref holdForSet, loadObject, setValue, 
                                    //parentType, arrayCounters, exampleObject);
                                }
                            }

                            break;
                    }

                    break;
            }
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

            

            var proxyLocal = s.ChildProxies[s.CurrentField];
            var proxyType = proxyLocal.FieldType;

            ////////////////////////////////////////////
            // PROXY->OUTSTREAM = MY STREAM
            ////////////////////////////////////////////
            //var streamSetter = proxyType.SetterOrDie(nameof(IProtoProxy<Object>.OutStream));

            //il.Emit(OpCodes.Ldloc, proxyLocal);
            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Callvirt, streamSetter);



            ////////////////////////////////////////////
            // PROXY->SCAN(CURRENT)
            ////////////////////////////////////////////
            var scanMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Scan),
                typeof(Stream), typeof(Int64));


            if (setCurrentValue != null)
                s.LoadCurrentValueOntoStack(il);
            else
                holdForSet = holdForSet ??  il.DeclareLocal(s.CurrentField.Type);

            //s.LoadCurrentValueOntoStack(il);

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

            //setCurrentValue(il);
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

