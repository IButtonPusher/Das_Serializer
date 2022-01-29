#if GENERATECODE

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;
using Das.Serializer.Proto;
using Das.Serializer.State;
using Reflection.Common;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        protected ILGenerator OpenPrintMethod(TypeBuilder bldr,
                                                       Type dtoType,
                                                       IEnumerable<IProtoFieldAccessor> fields,
                                                       IDictionary<Type, ProxiedInstanceField> typeProxies,
                                                       out ProtoPrintState? initialState)
        {
            var genericParent = typeof(ProtoDynamicBase<>).MakeGenericType(dtoType);

            var abstractMethod = genericParent.GetMethod(
                                     nameof(ProtoDynamicBase<Object>.Print))
                                 ?? throw new InvalidOperationException();

            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Print),
                MethodOverride, typeof(void), new[] {dtoType, typeof(Stream)});
            bldr.DefineMethodOverride(method, abstractMethod);

            var il = method.GetILGenerator();

            initialState = GetInitialState(dtoType, fields, typeProxies, il);

            return il;
        }

        private ProtoPrintState? GetInitialState(Type parentType,
                                                            IEnumerable<IProtoFieldAccessor> fields,
                                                            IDictionary<Type, ProxiedInstanceField> typeProxies,
                                                            ILGenerator il)
        {
        Action<ILGenerator> loadDto = parentType.IsValueType
            ? LoadValueDto
            : LoadReferenceDto;

        var fArr = fields.ToArray();

        if (fArr.Length == 0)
            return null;

        var startField = fArr[0];

        var state = new ProtoPrintState(il, false,
            fArr, parentType,
            loadDto, _types, this, startField,
            typeProxies, this);

        if (typeProxies.Count > 0)
            state.EnsureChildObjectStream();


        return state;
        }


        //protected override ProtoPrintState? GetInitialState(Type parentType,
        //                                         IEnumerable<IProtoFieldAccessor> fields,
        //                                         IDictionary<Type, ProxiedInstanceField> typeProxies,
        //                                         ILGenerator il)
        //{
        //    Action <ILGenerator> loadDto = parentType.IsValueType
        //        ? LoadValueDto
        //        : LoadReferenceDto;

        //    var fArr = fields.ToArray();

        //    if (fArr.Length == 0)
        //        return null;

        //    var startField = fArr[0];

        //    var state = new ProtoPrintState(il, false,
        //        fArr, parentType,
        //        loadDto, _types, this, startField,
        //        typeProxies, this);

        //    return state;
        //}

        private void AddPrintMethod(Type parentType,
                                    TypeBuilder bldr,
                                    IEnumerable<IProtoFieldAccessor> fields,
                                    IDictionary<Type, ProxiedInstanceField> typeProxies)
        {
            var il = OpenPrintMethod(bldr, parentType, fields, typeProxies, out var state);


            //var il = method.GetILGenerator();

            //var state = GetInitialState(parentType, fields, typeProxies, il);

            if (state == null)
                goto endOfMethod;


            //Action <ILGenerator> loadDto = parentType.IsValueType
            //    ? LoadValueDto
            //    : LoadReferenceDto;

            //var fArr = fields.ToArray();

            //if (fArr.Length == 0)
            //    goto endOfMethod;

            //var startField = fArr[0];

            //var state = new ProtoPrintState(il, false,
            //    fArr, parentType,
            //    loadDto, _types, this, startField,
            //    typeProxies, this);


            //if (typeProxies.Count > 0)
            //    state.EnsureChildObjectStream();

            var fieldIndex = 0;

            foreach (var protoField in state)
                /////////////////////////////////////////
            {
                AddFieldToPrintMethod(protoField);
            }
            /////////////////////////////////////////

            endOfMethod:
            il.Emit(OpCodes.Ret);
            //bldr.DefineMethodOverride(method, abstractMethod);
        }

        //private static void LoadReferenceDto(ILGenerator il) => il.Emit(OpCodes.Ldarg_1);

        //private static void LoadValueDto(ILGenerator il) => il.Emit(OpCodes.Ldarga, 1);


        //private void AddFieldToPrintMethod2(IProtoPrintState state)
        //{
        //    var ifFalse2 = VerifyValueIsNonDefault(state);

        //    switch (state.CurrentFieldAction)
        //    {
        //        case ProtoFieldAction.VarInt:
        //            PrintVarInt(state);
        //            break;

        //        case ProtoFieldAction.Primitive:
        //            PrintPrimitive(state);
        //            break;

        //        case ProtoFieldAction.String:
        //            PrintString(state, s => s.LoadCurrentFieldValueToStack());
        //            break;

        //        case ProtoFieldAction.ByteArray:
        //            PrintByteArray(state);
        //            break;

        //        case ProtoFieldAction.PackedArray:
        //            PrintAsPackedArray(state);
        //            break;

        //        case ProtoFieldAction.ChildObject:
        //            PrintChildObject(state, state.CurrentFieldHeader,
        //                _ => state.LoadCurrentFieldValueToStack(),
        //                state.CurrentField.Type);
        //            break;

        //        case ProtoFieldAction.ChildObjectCollection:
        //        case ProtoFieldAction.ChildPrimitiveCollection:
        //            PrintCollection(state, PrintEnumeratorCurrent);
        //            break;

        //        case ProtoFieldAction.Dictionary:
        //            PrintCollection(state, PrintKeyValuePair);
        //            break;

        //        case ProtoFieldAction.ChildObjectArray:
        //            PrintArray(state, PrintEnumeratorCurrent);
        //            break;

        //        case ProtoFieldAction.ChildPrimitiveArray:
        //            PrintCollection(state, PrintEnumeratorCurrent);

        //            break;

        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }

        //    state.IL.MarkLabel(ifFalse2);
        //}

        

        private void PrintChildObject(IProtoPrintState s,
                                      Byte[] headerBytes,
                                      Action<ILGenerator> loadObject,
                                      Type fieldType)
        {
            var il = s.IL;

            PrintHeaderBytes(headerBytes, s);

            var proxy = s.GetProxy(fieldType);
            var proxyField = proxy.ProxyField;
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
            il.Emit(OpCodes.Callvirt, GetStreamLength);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, WriteInt64);

            ////////////////////////////////////////////
            // COPY CHILD STREAM TO MAIN
            ////////////////////////////////////////////
            //reset stream
            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Callvirt, SetStreamPosition);

            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, CopyMemoryStream);


            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Callvirt, SetStreamLength);
        }

        private void PrintConstByte(Byte constVal,
                                    ILGenerator il)
        {
            var noStackDepth = il.DefineLabel();
            var endOfPrintConst = il.DefineLabel();


            //notPushed:
            il.MarkLabel(noStackDepth);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldc_I4_S, constVal);
            il.Emit(OpCodes.Callvirt, _writeStreamByte);

            il.MarkLabel(endOfPrintConst);
        }

        private void PrintHeaderBytes(Byte[] headerBytes,
                                      IProtoPrintState s)
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
                PrintConstByte(headerBytes[0], il);
        }


        private static Label VerifyValueIsNonDefault(IProtoPrintState s)
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
                    il.Emit(OpCodes.Brfalse, gotoIfFalse);

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


       
    }
}

#endif
