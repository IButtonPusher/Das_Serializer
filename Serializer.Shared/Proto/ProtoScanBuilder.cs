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
            Type genericParent, IEnumerable<IProtoFieldAccessor> fields, Object exampleObject)
        {
            var fieldArr = fields.ToArray();

            var buildDefault = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.BuildDefault));

            var methodArgs = new[] {typeof(Stream), typeof(Int64)};

            var abstractMethod = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.Scan), Const.PublicInstance, methodArgs);

            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Scan),
                MethodOverride, parentType, methodArgs);

            var il = method.GetILGenerator();

            ///////////////
            // VARIABLES
            //////////////
            var streamLength = il.DeclareLocal(typeof(Int64));
            var lastByte = il.DeclareLocal(typeof(Int32));

            var fieldByteArray = il.DeclareLocal(typeof(Byte[]));
            var endLabel = il.DefineLabel();
            var instantiated = il.DefineLabel();

            //local threadstatic byte[] buffer
            il.Emit(OpCodes.Ldsfld, _readBytes);
            il.Emit(OpCodes.Brtrue_S, instantiated);

            il.Emit(OpCodes.Ldc_I4, 256);
            il.Emit(OpCodes.Newarr, typeof(Byte));

            il.Emit(OpCodes.Stsfld, _readBytes);

            il.MarkLabel(instantiated);
            il.Emit(OpCodes.Ldsfld, _readBytes);
            il.Emit(OpCodes.Stloc, fieldByteArray);
            ////////////////////////////

            //////////////////////////////
            // INSTANTIATE RETURN VALUE
            /////////////////////////////
            var arrayCounters = new ProtoArrayInfo(_getStreamPosition, _types, _getPositiveInt32);

            il.Emit(OpCodes.Ldarg_0);
            var returnValue = InstantiateObjectToDefault(il, parentType, buildDefault, fieldArr,
                arrayCounters, exampleObject);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, _getStreamPosition);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, streamLength);
            /////////////////////////////

            /////////////////////////////
            AddPropertiesToScanMethod(fieldArr, parentType, il,
                ilg => ilg.Emit(OpCodes.Ldloc, returnValue),
                endLabel, fieldByteArray, lastByte, streamLength,
                arrayCounters, exampleObject);
            /////////////////////////////

            il.MarkLabel(endLabel);

            il.Emit(OpCodes.Ldloc, returnValue);
            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private void AddPropertiesToScanMethod(IEnumerable<IProtoFieldAccessor> fields,
            Type parentType, ILGenerator il, Action<ILGenerator> loadObject,
            Label afterPropertyLabel, LocalBuilder fieldByteArray, 
            LocalBuilder lastByte, LocalBuilder streamLength, 
            ProtoArrayInfo arrayCounters, Object exampleObject)
        {
            var fieldArr = fields.ToArray();

            var state = new ProtoScanState(il, fieldArr, null, parentType, loadObject,
                fieldByteArray, lastByte, exampleObject, arrayCounters, 
                //streamLength, 
                _utf8, _types);

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
                var lbl = AddScanProperty(state, whileLabel);
                //////////////////////////////////////////////////////////////////

                labelFarm[currentProp.Index] = lbl;
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
            //il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldloc, streamLength);

            il.Emit(OpCodes.Blt,getNextColumnIndexLabel);
            il.Emit(OpCodes.Br, afterPropertyLabel);
        }

        private static LocalBuilder InstantiateObjectToDefault(ILGenerator il, Type type,
            MethodBase buildDefault, IEnumerable<IProtoField> fields, 
            ProtoArrayInfo arrayBuilders, Object exampleObject)
        {
            var returnValue = il.DeclareLocal(type);

            switch (buildDefault)
            {
                case ConstructorInfo ctor:
                    il.Emit(OpCodes.Newobj, ctor);
                    break;
                case MethodInfo bdor:
                    il.Emit(OpCodes.Callvirt, bdor);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            il.Emit(OpCodes.Stloc, returnValue);

            foreach (var field in fields)
            {
                if (!field.IsRepeatedField)
                    continue;

                if (field.Type.IsArray)
                {
                    //if (!typeof(IEnumerable<Int32>).IsAssignableFrom(field.Type))
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

                var fieldCtor = field.Type.GetDefaultConstructorOrDie();

                il.Emit(OpCodes.Ldloc, returnValue);
                il.Emit(OpCodes.Newobj, fieldCtor);
                il.Emit(OpCodes.Callvirt, setter);
            }

            return returnValue;
        }

        private Label AddScanProperty(ProtoScanState s, Label afterPropertyLabel)
        {
            var il = s.IL;
            var currentProp = s.CurrentField;

            var setter = s.ParentType.SetterOrDie(currentProp.Name);

            LocalBuilder holdForSet = null;

            var caseEntryForThisProperty = s.IL.DefineLabel();
            s.IL.MarkLabel(caseEntryForThisProperty);

            s.SetCurrentValue = ill => ill.Emit(OpCodes.Callvirt, setter);

            //////////////////////////////////////////////////////////////////
            PopulateScanField(s, ref holdForSet!);
            //////////////////////////////////////////////////////////////////

            if (holdForSet != null)
            {
                s.LoadCurrentValueOntoStack(il);
                il.Emit(OpCodes.Ldloc, holdForSet);
                il.Emit(OpCodes.Callvirt, setter);
            }

            il.Emit(OpCodes.Br, afterPropertyLabel);

            return caseEntryForThisProperty;
        }

        private void PopulateScanField(ProtoScanState s, 
                ref LocalBuilder holdForSet)
        {
            var pv = s.CurrentField;

            var wireType = s.CurrentField.WireType;
            var il = s.IL;
            var currentProp = s.CurrentField;

            switch (pv.FieldAction)
            {
                case ProtoFieldAction.VarInt:
                case ProtoFieldAction.Primitive:
                    TryScanAsVarInt(s, ref holdForSet);
                    return;

                case ProtoFieldAction.String:
                    ScanString(s);
                    return;

                case ProtoFieldAction.ByteArray:
                    ScanByteArray(s, ref holdForSet);
                    return;

                case ProtoFieldAction.PackedArray:
                    TryScanAsPackedArray(s, ref holdForSet);
                    return;

                case ProtoFieldAction.ChildObject:
                    s.LoadCurrentValueOntoStack(il);
                    ScanChildObject(s);
                    s.SetCurrentValue(il);
                    return;

                case ProtoFieldAction.ChildObjectCollection:
                    TryScanAsNestedCollection(s);
                    return;

                case ProtoFieldAction.ChildObjectArray:
                    break;
                case ProtoFieldAction.Dictionary:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            switch (wireType)
            {
                case Const.VarInt:
                case ProtoWireTypes.Int64: //64-bit zb double
                case ProtoWireTypes.Int32:
                    TryScanAsVarInt(s,
                        //currentProp, fieldByteArray, lastByte,
                        ref holdForSet!);
                        //loadObject,
                        //setValue,
                        //parentType, arrayCounters, exampleObject);
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
                            il.Emit(OpCodes.Ldsfld, _readBytes);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
                            il.Emit(OpCodes.Callvirt, _readStreamBytes);
                            il.Emit(OpCodes.Pop);

                            //base.Utf8.GetString(fieldByteArray);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, _utf8);
                            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
                            il.Emit(OpCodes.Ldsfld, _readBytes);
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

                                break;
                            }

                            else if (TryScanAsPackedArray(s, ref holdForSet))
                                //il, currentProp, lastByte, 
                                //s.LoadCurrentValueOntoStack,
                                //setValue, ref holdForSet, exampleObject, parentType))
                                return;

                           
                            ///////////////////////
                            // NESTED PROTOCONTRACT
                            ///////////////////////
                            else if (!currentProp.IsRepeatedField)
                            {
                                var willAddToExisting = TryLoadTargetReference(s);
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

                                var holdForSetCopy = holdForSet;

                                throw new NotImplementedException();

                                //AddPropertiesToScanMethod(childFields, currentProp.Type, il,
                                //    ilg => ilg.Emit(OpCodes.Ldloc, holdForSetCopy), endOfPropLabel,
                                //    fieldByteArray, lastByte, propLength, arrayCounters,
                                //    exampleObject);

                                il.MarkLabel(endOfPropLabel);
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
                                else if (TryScanAsDictionary(s))
                                    //il, currentProp, fieldByteArray,
                                    //lastByte, loadObject, parentType, arrayCounters, exampleObject))
                                    break;
                                else
                                {
                                    ////////////////////
                                    // OTHER COLLECTIONS
                                    ////////////////////

                                    TryScanAsNestedCollection(s);
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
        
        private void ScanChildObject(ProtoScanState s)
        {
            var il = s.IL;

            var proxyLocal = s.ChildProxies[s.CurrentField];
            var proxyType = proxyLocal.LocalType;

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
            var scanMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Scan));

            il.Emit(OpCodes.Ldloc, proxyLocal);
            il.Emit(OpCodes.Ldarg_1);  //arg1 = input stream!
            
            //il.Emit(OpCodes.Ldloc, enumeratorCurrentValue);
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

