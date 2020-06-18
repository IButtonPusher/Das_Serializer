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
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void AddPrintMethod(Type parentType, TypeBuilder bldr, Type genericParent,
            IEnumerable<IProtoFieldAccessor> fields,
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
                loadDto, _types,
                _writeInt32, this, startField,
                typeProxies);


            if (typeProxies.Count > 0)
                state.EnsureChildObjectStream();

            foreach (var protoField in state)
            {
                /////////////////////////////////////////
                AddFieldToPrintMethod(protoField);
                /////////////////////////////////////////
            }

            endOfMethod:
            il.Emit(OpCodes.Ret);
            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private void AddFieldToPrintMethod(ProtoPrintState s)
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

            PrintHeaderBytes(headerBytes, s);

            var proxyField = s.GetProxy(s.CurrentField.Type);


            //var proxyField = s.ChildProxies[s.CurrentField];
            var proxyType = proxyField.FieldType;

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
            il.Emit(OpCodes.Call, _copyMemoryStream);


            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Callvirt, _setStreamLength);
        }

    }
}
