using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using Das.Extensions;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void AddPrintMethod(Type parentType, TypeBuilder bldr, Type genericParent,
            IEnumerable<IProtoFieldAccessor> fields,
            IDictionary<IProtoFieldAccessor, FieldBuilder> childProxyFields,
            IDictionary<Type, FieldBuilder> typeProxies)
        {
            var abstractMethod = genericParent.GetMethod(
                                     nameof(ProtoDynamicBase<Object>.Print))
                                 ?? throw new InvalidOperationException();

            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Print),
                MethodOverride, typeof(void), new[] {parentType, typeof(Stream)});

            var il = method.GetILGenerator();

            var instruction = parentType.IsValueType
                ? OpCodes.Ldarga
                : OpCodes.Ldarg;

            Action<ILGenerator> loadDto = ilg => ilg.Emit(instruction, 1);


            var fArr = fields.ToArray();

            if (fArr.Length == 0)
                goto endOfMethod;

            var startField = fArr[0];

            var state = new ProtoPrintState(il, false,
                fArr, parentType,
                loadDto, false, _types,
                _writeInt32, this, childProxyFields, startField,
                typeProxies);


            if (childProxyFields.Count > 0)
                state.EnsureChildObjectStream();


            foreach (var protoField in state)
            {
                /////////////////////////////////////////
                AddFieldToPrintMethod(protoField, loadDto);
                /////////////////////////////////////////
            }

            
            endOfMethod:
            il.Emit(OpCodes.Ret);
            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private void AddFieldToPrintMethod(ProtoPrintState s,
            Action<ILGenerator> loadObject)
        {
            var pv = s.CurrentField;

            var ifFalse2 = VerifyValueIsNonDefault(s);

            switch (pv.FieldAction)
            {
                case ProtoFieldAction.VarInt:
                    PrintVarInt(s);
                    break;

                case ProtoFieldAction.Primitive:
                    PrintPrimitive(s);
                    break;

                case ProtoFieldAction.String:
                    PrintString(s, (_s) => _s.LoadCurrentFieldValueToStack()); //P_0.StringField);
                    break;

                case ProtoFieldAction.ByteArray:
                    PrintByteArray(s);
                    break;

                case ProtoFieldAction.PackedArray:
                    PrintAsPackedArray(s);
                    break;

                case ProtoFieldAction.ChildObject:
                    PrintChildObject(s, s.CurrentField.HeaderBytes,
                        _ => s.LoadCurrentFieldValueToStack());
                    break;

                case ProtoFieldAction.ChildObjectCollection:
                case ProtoFieldAction.ChildPrimitiveCollection:
                    PrintCollection(s, PrintEnumeratorCurrent);
                    break;

                case ProtoFieldAction.Dictionary:
                    PrintCollection(s, PrintKeyValuePair);
                    break;

                case ProtoFieldAction.ChildObjectArray:
                    PrintCollection(s, PrintEnumeratorCurrent);
                    break;

                case ProtoFieldAction.ChildPrimitiveArray:
                    PrintCollection(s, PrintEnumeratorCurrent);
                    //TryPrintAsArray(s, pv.HeaderBytes);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            s.IL.MarkLabel(ifFalse2);

        }

        private void PrintHeaderBytes(Byte[] headerBytes,
            ProtoPrintState s)
        {
            var il = s.IL;
            var fieldByteArray = s.FieldByteArray;

            if (headerBytes.Length > 1)
            {
                il.Emit(OpCodes.Ldarg_0);

                if (!s.IsArrayMade)
                {
                    il.Emit(OpCodes.Ldc_I4_3);
                    il.Emit(OpCodes.Newarr, typeof(Byte));
                    il.Emit(OpCodes.Stloc, fieldByteArray);
                    s.IsArrayMade = true;
                }

                il.Emit(OpCodes.Ldloc, fieldByteArray);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldc_I4_S, headerBytes[0]);
                il.Emit(OpCodes.Stelem_I1);

                for (var c = 1; c < headerBytes.Length; c++)
                {
                    il.Emit(OpCodes.Ldloc, fieldByteArray);
                    il.Emit(OpCodes.Ldc_I4, c);
                    il.Emit(OpCodes.Ldc_I4_S, headerBytes[c]);
                    il.Emit(OpCodes.Stelem_I1);
                }

                il.Emit(OpCodes.Ldloc, fieldByteArray);
                il.Emit(OpCodes.Ldc_I4, headerBytes.Length);
                il.Emit(OpCodes.Call, _writeSomeBytes);
            }
            else
            {
                PrintConstByte(headerBytes[0], il); //, s.HasPushed);
            }
        }

        private void PrintConstByte(Byte constVal, ILGenerator il) //, Boolean? isPushed)
        {
            var noStackDepth = il.DefineLabel();
            var endOfPrintConst = il.DefineLabel();


            //notPushed:
            il.MarkLabel(noStackDepth);
            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldc_I4_S, constVal);
            il.Emit(OpCodes.Callvirt, _writeStreamByte);

            il.MarkLabel(endOfPrintConst);
        }

       

        private static Label VerifyValueIsNonDefault(ProtoPrintState s)
        {
            var il = s.IL;
            var propertyType = s.CurrentField.Type;

            var gotoIfFalse = il.DefineLabel();

            if (propertyType.IsPrimitive)
            {
                s.LoadCurrentFieldValueToStack();

                if (propertyType == typeof(Double))
                {
                    il.Emit(OpCodes.Ldc_R8, 0.0);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Brtrue, gotoIfFalse);
                }
                else
                {
                    il.Emit(OpCodes.Brfalse, gotoIfFalse);
                }

                goto done;
            }

            var countProp = propertyType.GetProperty(nameof(IList.Count));
            if (countProp == null || !(countProp.GetGetMethod() is { } countGetter))
                goto done;

            s.LoadCurrentFieldValueToStack();

            
            il.Emit(OpCodes.Callvirt, countGetter);
            il.Emit(OpCodes.Brfalse, gotoIfFalse);

            done:
            return gotoIfFalse;
        }

       

        private void PrintChildObject(ProtoPrintState s,
            Byte[] headerBytes,
            Action<ILGenerator> loadObject)
        {
            var il = s.IL;

            //PrintHeaderBytes(s.CurrentField.HeaderBytes, s);
            PrintHeaderBytes(headerBytes, s);

            var proxyField = s.ChildProxies[s.CurrentField];
            var proxyType = proxyField.FieldType;


            ////////////////////////////////////////////
            // PROXY->OUTSTREAM = CHILDSTREAM
            ////////////////////////////////////////////
            //var streamSetter = proxyType.SetterOrDie(nameof(IProtoProxy<Object>.OutStream));

            //il.Emit(OpCodes.Ldloc, proxyLocal);
            //il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            //il.Emit(OpCodes.Callvirt, streamSetter);


            ////////////////////////////////////////////
            // PROXY->PRINT(CURRENT)
            ////////////////////////////////////////////
            var printMethod = proxyType.GetMethodOrDie(nameof(IProtoProxy<Object>.Print));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, proxyField);
            loadObject(il);

            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);

            il.Emit(OpCodes.Call, printMethod);


            ////////////////////////////////////////////
            // PRINT LENGTH OF CHILD STREAM
            ////////////////////////////////////////////
            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Callvirt, _getStreamLength);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, _writeInt64);

            ////////////////////////////////////////////
            // COPY CHILD STREAM TO MAIN
            ////////////////////////////////////////////
            //reset stream
            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Callvirt, _setStreamPosition);


            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);

            il.Emit(OpCodes.Ldarg_2);

            //il.Emit(OpCodes.Ldc_I4, 4096);
            il.Emit(OpCodes.Call, _copyMemoryStream);
            //il.Emit(OpCodes.Callvirt, _copyStreamTo);


            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Callvirt, _setStreamLength);
        }


        private void AddObtainableValueToPrintMethod(ProtoPrintState s,
                Action<ILGenerator> loadValue)
        {
            var il = s.IL;

            il.Emit(OpCodes.Ldarg_0);

            if (TryPrintAsVarInt(s, loadValue))
                //il, ref isArrayMade, fieldByteArray,
                //ref localBytes,
                //ref localString, utfField,
                //code, wireType, type,
                //loadValue, loadObject, ref hasPushed))
                return;

            var localBytes = s.LocalBytes;
            var type = s.CurrentField.Type;

            switch (s.CurrentField.WireType)
            {

                case ProtoWireTypes.LengthDelimited:
                    switch (s.CurrentField.TypeCode)
                    {

                        case TypeCode.String:
                            ////////////
                            // STRING
                            ///////////
                            s.LocalString ??= il.DeclareLocal(typeof(String));

                            var localString = s.LocalString;

                            loadValue(il);
                            il.Emit(OpCodes.Stloc, localString);

                            //il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldsfld, _utf8);
                            il.Emit(OpCodes.Ldloc, localString);

                            il.Emit(OpCodes.Callvirt, _getStringBytes);
                            il.Emit(OpCodes.Stloc, localBytes);

                            il.Emit(OpCodes.Ldloc, localBytes);
                            il.Emit(OpCodes.Call, _getArrayLength);
                            s.WriteInt32();
                            //il.Emit(OpCodes.Call, _writeInt32);

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldloc, localBytes);
                            il.Emit(OpCodes.Call, _writeBytes);

                            break;
                        case TypeCode.Object:

                            ///////////
                            // BYTE [ARRAY]
                            ///////////
                            if (type == Const.ByteArrayType)
                            {
                                loadValue(il);
                                il.Emit(OpCodes.Stloc, localBytes);

                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldloc, localBytes);
                                il.Emit(OpCodes.Call, _getArrayLength);
                                s.WriteInt32();
                                //il.Emit(OpCodes.Call, _writeInt32);

                                il.Emit(OpCodes.Ldloc, localBytes);
                                il.Emit(OpCodes.Call, _writeBytes);

                                break;
                            }

                            var localForPropVal = il.DeclareLocal(type);

                            loadValue(il);
                            il.Emit(OpCodes.Stloc, localForPropVal);

                            //var subFields = GetProtoFields(type);

                            s.HasPushed = true;
                            throw new NotImplementedException();
                            //il.Emit(OpCodes.Callvirt, _push);
                            //il.Emit(OpCodes.Pop);

                            //var state = new ProtoPrintState(s, subFields, type,
                            //    ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal), _types, _writeInt32,
                            //    this);

                            //foreach (var sub in state)
                            //{
                            //    AddFieldToPrintMethod(sub, ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal));
                            //    s.MergeLocals(sub);
                            //}

                            ////AddFieldsToPrintMethod(s, ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal));
                            ////    //il, ref isArrayMade, ref localString,
                            //////    fieldByteArray, ref localBytes, subFields, type, utfField,
                            //////    ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal),
                            //////    ref hasPushed);

                            //il.Emit(OpCodes.Ldarg_0);
                            //throw new NotImplementedException();
                            ////il.Emit(OpCodes.Callvirt, _pop);

                            //il.Emit(OpCodes.Pop);


                            break;
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
