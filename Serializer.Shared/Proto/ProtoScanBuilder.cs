using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private static MethodInfo SetOrDie(Type type, String property, BindingFlags flags =
            BindingFlags.Public | BindingFlags.Instance)
        {
            return type.GetProperty(property, flags)?.GetSetMethod() ??
                   throw new InvalidOperationException();
        }

        private void AddScanMethod(Type parentType, TypeBuilder bldr,
            Type genericParent, IEnumerable<IProtoField> fields)
        {
            // var args = MethodAttributes.Public |
            //            MethodAttributes.HideBySig |
            //            MethodAttributes.Virtual |
            //            MethodAttributes.CheckAccessOnOverride
            //            | MethodAttributes.Final;

            var buildDefault = GetMethodOrDie(genericParent,
                nameof(ProtoDynamicBase<Object>.BuildDefault));

            var abstractMethod = GetMethodOrDie(genericParent, 
                nameof(ProtoDynamicBase<Object>.Scan));


            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Scan),
                MethodOverride, parentType, new[] {typeof(Stream)});

            var il = method.GetILGenerator();

            ///////////////
            // VARIABLES
            //////////////
            var streamLength = il.DeclareLocal(typeof(Int64));
            var lastByte = il.DeclareLocal(typeof(Int32));
            
            var returnValue = il.DeclareLocal(parentType);
            var fieldByteArray = il.DeclareLocal(typeof(Byte[]));

            var endLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4, 256);
            il.Emit(OpCodes.Newarr, typeof(Byte));
            il.Emit(OpCodes.Stloc, fieldByteArray);
            ////////////////////////////

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, buildDefault);
            il.Emit(OpCodes.Stloc, returnValue);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, _getStreamLength);
            il.Emit(OpCodes.Stloc, streamLength);

            AddPropertiesToScanMethod(fields, parentType, il, 
                ilg => ilg.Emit(OpCodes.Ldloc, returnValue), 
                endLabel, endLabel, endLabel, 
                fieldByteArray, lastByte, streamLength);

            il.MarkLabel(endLabel);

            il.Emit(OpCodes.Ldloc, returnValue);
            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

        // private void AddSubPropertiesToScanMethod(IEnumerable<IProtoField> fields,
        //     Type parentType, ILGenerator il, Action<ILGenerator> loadObject,
        //     Label afterPropertyLabel, 
        //     Label unknownFieldIndexLabel,
        //     Label outOfBytesLabel,
        //     LocalBuilder fieldByteArray,
        //     LocalBuilder lastByte, 
        //     LocalBuilder streamLength)
        // {
        //     var fArr = fields.ToArray();
        //     
        //     //iterate over a maximum amount of fields corresponding to what this type has
        //     var fieldCount = il.DeclareLocal(typeof(Int32));
        //     il.Emit(OpCodes.Ldc_I4, fArr.Length);
        //     il.Emit(OpCodes.Stloc, fieldCount);
        //
        //     var currentField = il.DeclareLocal(typeof(Int32));
        //     il.Emit(OpCodes.Ldc_I4_0);
        //     il.Emit(OpCodes.Stloc, currentField);
        //
        //     var checkIfMoreFieldsLabel = il.DefineLabel();
        //
        //     var continueLabel = AddPropertiesToScanMethod(fArr, parentType, il, loadObject, 
        //         checkIfMoreFieldsLabel, unknownFieldIndexLabel, outOfBytesLabel, 
        //         streamLength, fieldByteArray, lastByte);
        //
        //     il.MarkLabel(checkIfMoreFieldsLabel);
        //     il.Emit(OpCodes.Ldloc, currentField);
        //     il.Emit(OpCodes.Ldc_I4_1);
        //     il.Emit(OpCodes.Add);
        //     il.Emit(OpCodes.Stloc, currentField);
        //     il.Emit(OpCodes.Ldloc, currentField);
        //     il.Emit(OpCodes.Ldloc, fieldCount);
        //
        //     il.Emit(OpCodes.Bge, afterPropertyLabel);
        //
        //     il.Emit(OpCodes.Br, continueLabel);
        // }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="afterPropertyLabel"></param>
        /// <param name="unknownFieldIndexLabel"></param>
        /// <param name="outOfBytesLabel"></param>
        /// <returns>A label where a check would be done for more bytes in the stream.
        /// If there are more bytes, the next field would be obtained and either switched
        /// into a setter or redirected to <param name="unknownFieldIndexLabel" /></returns>
        private void AddPropertiesToScanMethod(IEnumerable<IProtoField> fields,
            Type parentType, ILGenerator il, Action<ILGenerator> loadObject,
            Label afterPropertyLabel, 
            Label unknownFieldIndexLabel,
            Label outOfBytesLabel,
            LocalBuilder fieldByteArray, 
            LocalBuilder lastByte, 
            LocalBuilder streamLength)
        {

            var switchFieldIndexLabel = il.DefineLabel();
            var whileLabel = il.DefineLabel();

            /////////////////////////////
            // 1. GET CURRENT FIELD INDEX
            /////////////////////////////
            var getNextColumnIndexLabel = AddLoadCurrentFieldIndex(il, switchFieldIndexLabel);

            var labelFarm = new Dictionary<Int32, Label>();
            var max = 0;

            /////////////////////////////////
            // 3. LOGIC PER FIELD VIA SWITCH
            /////////////////////////////////
            foreach (var currentProp in fields)
            {
                var lbl = AddScanProperty(
                    parentType, currentProp, il, loadObject, 
                    whileLabel, fieldByteArray, lastByte);

                labelFarm[currentProp.Index] = lbl;
                max = Math.Max(max, currentProp.Index);
            }

            // build the jump based on observations but fill in gaps
            var jt = new Label[max + 1];
            for (var c = 0; c <= max; c++)
            {
                if (labelFarm.TryGetValue(c, out var baa))
                    jt[c] = baa;
                else
                    jt[c] = unknownFieldIndexLabel;
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

            ////
            // il.Emit(OpCodes.Ldarg_0);
            // il.Emit(OpCodes.Ldarg_1);
            // il.Emit(OpCodes.Callvirt, _getStreamPosition);
            // il.Emit(OpCodes.Box, typeof(Int64));
            // il.Emit(OpCodes.Ldloc, streamLength);
            // il.Emit(OpCodes.Box, typeof(Int64));
            // il.Emit(OpCodes.Call, _debugWriteline);
            ////
            
            
            il.Emit(OpCodes.Blt,getNextColumnIndexLabel);

            il.Emit(OpCodes.Br, outOfBytesLabel);
        }

        private Label AddScanProperty(Type parentType, IProtoField currentProp,
            ILGenerator il, Action<ILGenerator> loadObject, 
            Label afterPropertyLabel,
            LocalBuilder fieldByteArray, LocalBuilder lastByte)
        {
            var wireType = currentProp.WireType;
            var currentType = currentProp.Type;
            var setter = SetOrDie(parentType, currentProp.Name);
                
            LocalBuilder holdForSet = null;

            var label = il.DefineLabel();
            il.MarkLabel(label);            

            switch (wireType)
            {
                ///////////////
                // INT32
                ///////////////
                case ProtoWireTypes.Varint when currentType == Const.IntType:
                    holdForSet = il.DeclareLocal(typeof(Int32));
                    //il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);                    
                    
                    il.Emit(OpCodes.Call, _getInt32);
                    il.Emit(OpCodes.Stloc, holdForSet);
                    break;

                ///////////////
                // SINGLE BYTE
                ///////////////
                case ProtoWireTypes.Varint when currentType == Const.ByteType:
                    il.Emit(OpCodes.Ldarg_1);

                    holdForSet = il.DeclareLocal(typeof(Byte));
                    il.Emit(OpCodes.Callvirt, _readStreamByte);
                    il.Emit(OpCodes.Stloc, holdForSet);

                    break;

                /////////////
                // VARINT
                /////////////
                case Const.VarInt:
                    throw new NotImplementedException();
                    //propValue = _feeder.GetVarInt(currentType);
                    break;

                case ProtoWireTypes.Int64: //64-bit zb double
                case ProtoWireTypes.Int32:

                    //single, double
                    switch (currentProp.TypeCode)
                    {
                        /////////////
                        // SINGLE
                        /////////////
                        case TypeCode.Single:
                            il.Emit(OpCodes.Ldarg_1);

                            holdForSet = il.DeclareLocal(typeof(Single));

                            il.Emit(OpCodes.Ldloc, fieldByteArray);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldc_I4_4);
                            il.Emit(OpCodes.Callvirt, _readStreamBytes);
                            il.Emit(OpCodes.Pop);

                            il.Emit(OpCodes.Ldloc, fieldByteArray);

                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Call, _bytesToSingle);
                            il.Emit(OpCodes.Stloc, holdForSet);
                            break;

                        /////////////
                        // DOUBLE
                        /////////////
                        case TypeCode.Double:
                            il.Emit(OpCodes.Ldarg_1);


                            holdForSet = il.DeclareLocal(typeof(Double));

                            il.Emit(OpCodes.Ldloc, fieldByteArray);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldc_I4_8);
                            il.Emit(OpCodes.Callvirt, _readStreamBytes);
                            il.Emit(OpCodes.Pop);

                            il.Emit(OpCodes.Ldloc, fieldByteArray);

                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Call, _bytesToDouble);
                            il.Emit(OpCodes.Stloc, holdForSet);
                            break;
                        default:
                            throw new NotImplementedException();
                            //propValue = _feeder.GetPrimitive(currentType);
                            break;
                    }


                    break;

                case ProtoWireTypes.LengthDelimited when currentProp.Type.IsPrimitive:
                    throw new NotImplementedException();
                    // propValue = _feeder.GetPrimitive(currentType);
                    break;

                case ProtoWireTypes.LengthDelimited:

                    //string, byte[]
                    switch (currentProp.TypeCode)
                    { 
                        ///////////
                        // STRING
                        ///////////
                        case TypeCode.String:
                            il.Emit(OpCodes.Ldarg_1);
                            holdForSet = il.DeclareLocal(typeof(String));

                            //Get length of string's bytes
                            //il.Emit(OpCodes.Ldarg_0);
                             il.Emit(OpCodes.Ldarg_1);
                             il.Emit(OpCodes.Call, _getPositiveInt32);
                             il.Emit(OpCodes.Stloc, lastByte);
                            //
                            //read bytes into buffer field
                            il.Emit(OpCodes.Ldloc, fieldByteArray);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldloc, lastByte);
                            il.Emit(OpCodes.Callvirt, _readStreamBytes);
                            il.Emit(OpCodes.Pop);                            
                            
                             //base.Utf8.GetString(fieldByteArray);
                             il.Emit(OpCodes.Ldarg_0);
                             il.Emit(OpCodes.Ldfld, _utf8);
                             il.Emit(OpCodes.Ldloc, fieldByteArray);
                             il.Emit(OpCodes.Ldc_I4_0);
                             il.Emit(OpCodes.Ldloc, lastByte);
                             il.Emit(OpCodes.Call, _bytesToString);
                            il.Emit(OpCodes.Stloc, holdForSet);

                            break;

                        case TypeCode.Object:

                            if (currentProp.Type == Const.ByteArrayType)
                            {
                                ////////////////
                                // BYTE [ARRAY]
                                ////////////////
                                il.Emit(OpCodes.Ldarg_1);
                                holdForSet = il.DeclareLocal(typeof(Byte[]));

                                //Get length of the array
                                //il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Call, _getPositiveInt32);

                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Stloc, lastByte);
                                il.Emit(OpCodes.Newarr, typeof(Byte));
                                il.Emit(OpCodes.Stloc, holdForSet);                                

                                //read bytes into buffer field
                                il.Emit(OpCodes.Ldloc, holdForSet);
                                il.Emit(OpCodes.Ldc_I4_0);
                                il.Emit(OpCodes.Ldloc, lastByte);
                                il.Emit(OpCodes.Callvirt, _readStreamBytes);
                                
                                il.Emit(OpCodes.Pop);
                            }
                            else
                            {
                                ///////////////////////
                                // NESTED PROTOCONTRACT
                                ///////////////////////

                                //  holdForSet = il.DeclareLocal(currentProp.Type);
                                //
                                //  //instantiate an object of the property 
                                //  var ctor = currentProp.Type.GetConstructor(Type.EmptyTypes)
                                //      ?? throw new MissingMethodException();
                                //  il.Emit(OpCodes.Newobj, ctor);
                                //  il.Emit(OpCodes.Stloc, holdForSet);
                                //
                                //  var childFields = GetProtoFields(currentProp.Type);
                                //
                                //  var beginForLoop = il.DefineLabel();
                                //  var incrementIndex = il.DefineLabel();
                                //  var endOfForLoop = il.DefineLabel();
                                //
                                // // var switchLabel = CreateSwitchStatement(childFields, 
                                // //        currentProp.Type, il,
                                // //        ilg => ilg.Emit(OpCodes.Ldloc, holdForSet),
                                // //        incrementIndex, endOfForLoop, outOfBytesLabel, fieldByteArray,
                                // //        lastByte, streamLength);
                                //
                                //  /////////////////
                                //  // FOR INDEX = 0
                                //  ////////////////
                                //  var currentFieldIndex = il.DeclareLocal(typeof(Int32));
                                //  il.Emit(OpCodes.Ldc_I4_0);
                                //  il.Emit(OpCodes.Stloc, currentFieldIndex);
                                //
                                //  /////////////////////////
                                //  // FOR INDEX < FIELDCOUNT
                                //  /////////////////////////
                                //  il.MarkLabel(beginForLoop);
                                //
                                //  il.Emit(OpCodes.Ldloc, currentFieldIndex);
                                //  il.Emit(OpCodes.Ldc_I4, childFields.Count);
                                //  il.Emit(OpCodes.Bge, endOfForLoop);
                                //
                                //  var switchLabel = CreateSwitchStatement(childFields, 
                                //      currentProp.Type, il,
                                //      ilg => ilg.Emit(OpCodes.Ldloc, holdForSet),
                                //      incrementIndex, endOfForLoop, outOfBytesLabel, fieldByteArray,
                                //      lastByte, streamLength);
                                //
                                //
                                //  il.Emit(OpCodes.Br, switchLabel);
                                //
                                //  //////////////
                                //  // INDEX++
                                //  /////////////
                                //  il.MarkLabel(incrementIndex);
                                //  il.Emit(OpCodes.Ldloc, currentFieldIndex);
                                //  il.Emit(OpCodes.Ldc_I4_1);
                                //  il.Emit(OpCodes.Add);
                                //  il.Emit(OpCodes.Stloc, currentFieldIndex);
                                //  il.Emit(OpCodes.Br, beginForLoop);
                                //
                                // il.MarkLabel(endOfForLoop);

                                ///////////////////////
                                // NESTED PROTOCONTRACT
                                ///////////////////////

                                holdForSet = il.DeclareLocal(currentProp.Type);

                                //instantiate an object of the property 
                                var ctor = currentProp.Type.GetConstructor(Type.EmptyTypes)
                                    ?? throw new MissingMethodException();
                                il.Emit(OpCodes.Newobj, ctor);
                                il.Emit(OpCodes.Stloc, holdForSet);

                                var childFields = GetProtoFields(currentProp.Type);
                                var endOfPropLabel = il.DefineLabel();
                                var propLength = il.DeclareLocal(typeof(Int64));

                                
                                il.Emit(OpCodes.Ldarg_1);
                                il.Emit(OpCodes.Callvirt, _getStreamPosition);
                                //il.Emit(OpCodes.Conv_I4);
                                
                                il.Emit(OpCodes.Ldarg_1);
                                 il.Emit(OpCodes.Call, _getPositiveInt64);

                                 il.Emit(OpCodes.Add);

                                 il.Emit(OpCodes.Stloc, propLength);

                                 il.Emit(OpCodes.Ldarg_1);
                                 il.Emit(OpCodes.Callvirt, _getStreamPosition);
                                 //il.Emit(OpCodes.Conv_I4);
                                 il.Emit(OpCodes.Ldloc, propLength);

                                 il.Emit(OpCodes.Bge, endOfPropLabel);

                                 // il.Emit(OpCodes.Ldarg_0);
                                 // il.Emit(OpCodes.Ldstr, "istart " + currentProp.Name);
                                 // il.Emit(OpCodes.Ldloc, propLength);
                                 // il.Emit(OpCodes.Box, typeof(Int32));
                                 // il.Emit(OpCodes.Call, _debugWriteline);


                                AddPropertiesToScanMethod(childFields, currentProp.Type, il,
                                    ilg => ilg.Emit(OpCodes.Ldloc, holdForSet), endOfPropLabel,
                                    endOfPropLabel, endOfPropLabel, fieldByteArray,
                                    lastByte, propLength);

                                

                                il.MarkLabel(endOfPropLabel);
                                
                                // il.Emit(OpCodes.Ldarg_0);
                                // il.Emit(OpCodes.Ldstr, "iend " + currentProp.Name);
                                // il.Emit(OpCodes.Ldloc, lastByte);
                                // il.Emit(OpCodes.Box, typeof(Int32));
                                // il.Emit(OpCodes.Call, _debugWriteline);
                            }


                            break;
                    }

                    break;
            }
            

            if (holdForSet != null)
            {
                loadObject(il);
                il.Emit(OpCodes.Ldloc, holdForSet);
                il.Emit(OpCodes.Callvirt, setter);

                // il.Emit(OpCodes.Ldarg_0);
                // loadObject(il);
                // il.Emit(OpCodes.Ldloc, holdForSet);
                // if (holdForSet.LocalType.IsValueType)
                //     il.Emit(OpCodes.Box, holdForSet.LocalType);
                //
                // il.Emit(OpCodes.Call, _debugWriteline);
            }

           il.Emit(OpCodes.Br, afterPropertyLabel);

            return label;
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
            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getColumnIndex);
            il.Emit(OpCodes.Br, goNextLabel);

            return nextFieldIndexLabel;
        }

        // private Label CreateSwitchStatement(IEnumerable<IProtoField> fields,
        //     Type parentType, ILGenerator il, Action<ILGenerator> loadObject,
        //     Label afterProperty,
        //     Label unknownFieldIndexLabel,
        //     Label outOfBytesLabel,
        //     LocalBuilder fieldByteArray,
        //     LocalBuilder lastByte, 
        //     LocalBuilder streamLength)
        // {
        //     var switchFieldIndexLabel = il.DefineLabel();
        //
        //     var labelFarm = new Dictionary<Int32, Label>();
        //     var max = 0;
        //
        //     /////////////////////////////////
        //     // 3. LOGIC PER FIELD VIA SWITCH
        //     /////////////////////////////////
        //     foreach (var currentProp in fields)
        //     {
        //         var lbl = AddScanProperty(
        //             parentType, currentProp, il, loadObject, 
        //             afterProperty,
        //             fieldByteArray, lastByte);
        //
        //         labelFarm[currentProp.Index] = lbl;
        //         max = Math.Max(max, currentProp.Index);
        //     }
        //
        //     // build the jump based on observations but fill in gaps
        //     var jt = new Label[max + 1];
        //     for (var c = 0; c <= max; c++)
        //     {
        //         if (labelFarm.TryGetValue(c, out var baa))
        //             jt[c] = baa;
        //         else
        //             jt[c] = unknownFieldIndexLabel;
        //     }
        //
        //     il.MarkLabel(switchFieldIndexLabel);
        //     il.Emit(OpCodes.Ldarg_1);
        //     il.Emit(OpCodes.Call, _getColumnIndex);
        //     il.Emit(OpCodes.Switch, jt);
        //
        //     return switchFieldIndexLabel;
        // }

    }
}

