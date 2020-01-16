using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Extensions;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
      

        private void AddScanMethod(Type parentType, TypeBuilder bldr,
            Type genericParent, IEnumerable<IProtoField> fields)
        {
            var fieldArr = fields.ToArray();

            var buildDefault = genericParent.GetMethodOrDie(
                nameof(ProtoDynamicBase<Object>.BuildDefault));

            var abstractMethod = genericParent.GetMethodOrDie( 
                nameof(ProtoDynamicBase<Object>.Scan));


            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Scan),
                MethodOverride, parentType, new[] {typeof(Stream)});

            var il = method.GetILGenerator();

            ///////////////
            // VARIABLES
            //////////////
            var streamLength = il.DeclareLocal(typeof(Int64));
            var lastByte = il.DeclareLocal(typeof(Int32));

            var fieldByteArray = il.DeclareLocal(typeof(Byte[]));

            var endLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4, 256);
            il.Emit(OpCodes.Newarr, typeof(Byte));
            il.Emit(OpCodes.Stloc, fieldByteArray);
            ////////////////////////////

            
            //////////////////////////////
            // INSTANTIATE RETURN VALUE
            /////////////////////////////
            il.Emit(OpCodes.Ldarg_0);
            var returnValue = InstantiateObjectToDefault(il, parentType, buildDefault, fieldArr);

            il.Emit(OpCodes.Ldarg_1);
             il.Emit(OpCodes.Callvirt, _getStreamLength);
             il.Emit(OpCodes.Stloc, streamLength);
            
            AddPropertiesToScanMethod(fieldArr, parentType, il, 
                ilg => ilg.Emit(OpCodes.Ldloc, returnValue), 
                endLabel, fieldByteArray, lastByte, streamLength);

            il.MarkLabel(endLabel);

            il.Emit(OpCodes.Ldloc, returnValue);
            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

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
            Label afterPropertyLabel, LocalBuilder fieldByteArray, 
            LocalBuilder lastByte, LocalBuilder streamLength)
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

        private LocalBuilder InstantiateObjectToDefault(ILGenerator il, Type type,
            MethodBase buildDefault, IEnumerable<IProtoField> fields)
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
                
                var setter = type.SetterOrDie(field.Name);
                var fieldCtor = field.Type.GetConstructor(Type.EmptyTypes)
                       ?? throw new MissingMethodException();

                il.Emit(OpCodes.Ldloc, returnValue);
                il.Emit(OpCodes.Newobj, fieldCtor);
                il.Emit(OpCodes.Callvirt, setter);
            }

            return returnValue;
        }

        private Label AddScanProperty(Type parentType, IProtoField currentProp,
            ILGenerator il, Action<ILGenerator> loadObject,
            Label afterPropertyLabel, LocalBuilder fieldByteArray, LocalBuilder lastByte)
        {
            var setter = parentType.SetterOrDie(currentProp.Name);

            LocalBuilder holdForSet = null;

            var label = il.DefineLabel();
            il.MarkLabel(label);

            PopulateScanField(il, currentProp, fieldByteArray, lastByte, 
                ref holdForSet,loadObject, parentType);

            if (holdForSet != null)
            {
                loadObject(il);
                il.Emit(OpCodes.Ldloc, holdForSet);
                il.Emit(OpCodes.Callvirt, setter);
            }

            il.Emit(OpCodes.Br, afterPropertyLabel);

            return label;
        }

        private void PopulateScanField(ILGenerator il,
            IProtoField currentProp,LocalBuilder fieldByteArray,
            LocalBuilder lastByte, ref LocalBuilder holdForSet,
            Action<ILGenerator> loadObject, Type parentType)
        {
            var wireType = currentProp.WireType;

            switch (wireType)
            {
                /////////////
                // VARINT
                /////////////
                case Const.VarInt:
                    switch (currentProp.TypeCode)
                    {
                        ///////////////
                        // INT32
                        ///////////////
                        case TypeCode.Int32:
                            holdForSet = holdForSet ?? il.DeclareLocal(typeof(Int32));
                            il.Emit(OpCodes.Ldarg_1);

                            il.Emit(OpCodes.Call, _getInt32);
                            il.Emit(OpCodes.Stloc, holdForSet);
                            break;
                        ///////////////
                        // SINGLE BYTE
                        ///////////////
                        case TypeCode.Byte:
                            il.Emit(OpCodes.Ldarg_1);

                            holdForSet = holdForSet ??il.DeclareLocal(typeof(Byte));
                            il.Emit(OpCodes.Callvirt, _readStreamByte);
                            il.Emit(OpCodes.Stloc, holdForSet);

                            break;
                        ///////////////
                        // INT64
                        ///////////////
                        case TypeCode.Int64:
                            ExtractInt64FromArg1Stream(il, ref holdForSet);
                            break;
                    }
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

                            holdForSet = holdForSet ?? il.DeclareLocal(typeof(Single));

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

                            holdForSet = holdForSet ?? il.DeclareLocal(typeof(Double));

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
                    }

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
                        }
                            break;

                        case TypeCode.Object:

                            ////////////////
                            // BYTE [ARRAY]
                            ////////////////
                            if (currentProp.Type == Const.ByteArrayType)
                            {
                               
                                il.Emit(OpCodes.Ldarg_1);
                                holdForSet = holdForSet ?? il.DeclareLocal(typeof(Byte[]));

                                //Get length of the array
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

                                break;
                            }

                            ///////////////////////
                            // NESTED PROTOCONTRACT
                            ///////////////////////
                            if (!currentProp.IsRepeatedField)
                            {
                                //instantiate an object of the property 
                                var ctor = currentProp.Type.GetConstructor(Type.EmptyTypes)
                                           ?? throw new MissingMethodException();

                                var childFields = GetProtoFields(currentProp.Type);

                                holdForSet = InstantiateObjectToDefault(il, currentProp.Type, ctor,
                                    childFields);

                                
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

                                AddPropertiesToScanMethod(childFields, currentProp.Type, il,
                                    ilg => ilg.Emit(OpCodes.Ldloc, holdForSetCopy), endOfPropLabel,
                                    fieldByteArray, lastByte, propLength);

                                il.MarkLabel(endOfPropLabel);
                            }

                            //////////////
                            // COLLECTION
                            //////////////
                            else
                            {
                                if (typeof(IDictionary).IsAssignableFrom(currentProp.Type))
                                {
                                    //////////////
                                    // DICTIONARY
                                    //////////////
                                    var info = new ProtoDictionaryInfo(currentProp.Type, _types);

                                    if (!currentProp.Type.TryGetMethod(nameof(IList.Add), 
                                            out var adder, info.KeyType, info.ValueType) &&
                                        !currentProp.Type.TryGetMethod(
                                            nameof(ConcurrentDictionary<Object, Object>.TryAdd), 
                                            out adder, info.KeyType, info.ValueType))
                                        throw new InvalidOperationException();

                                    //discard total kvp length
                                    il.Emit(OpCodes.Ldarg_1);
                                    il.Emit(OpCodes.Call, _getPositiveInt32);
                                    il.Emit(OpCodes.Pop);

                                    //discard key header?
                                    il.Emit(OpCodes.Ldarg_1);
                                    il.Emit(OpCodes.Call, _getPositiveInt32);
                                    il.Emit(OpCodes.Pop);


                                    LocalBuilder keyLocal = null;
                                    LocalBuilder valueLocal = null;

                                    PopulateScanField(il, info.Key, fieldByteArray,
                                        lastByte, ref keyLocal, loadObject, currentProp.Type);

                                    //discard value header?
                                    il.Emit(OpCodes.Ldarg_1);
                                    il.Emit(OpCodes.Call, _getPositiveInt32);
                                    il.Emit(OpCodes.Pop);

                                    PopulateScanField(il, info.Value, fieldByteArray,
                                        lastByte, ref valueLocal, loadObject, currentProp.Type);
                                    //
                                    var getter = parentType.GetterOrDie(currentProp.Name);
                                    //
                                    loadObject(il);
                                    il.Emit(OpCodes.Callvirt, getter);
                                   
                                    il.Emit(OpCodes.Ldloc, keyLocal);
                                    il.Emit(OpCodes.Ldloc, valueLocal);
                                    il.Emit(OpCodes.Call, adder);
                                    if (adder.ReturnType != typeof(void))
                                        il.Emit(OpCodes.Pop);
                                }
                            }


                            break;
                    }

                    break;
            }
          
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

