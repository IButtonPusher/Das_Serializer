using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Extensions;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void AddPrintMethod(Type parentType, TypeBuilder bldr, Type genericParent,
            FieldInfo utfField, IEnumerable<IProtoField> fields)
        {
            var abstractMethod = genericParent.GetMethod(
                                     nameof(ProtoDynamicBase<Object>.Print))
                                 ?? throw new InvalidOperationException();

            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Print),
                MethodOverride, typeof(void), new[] {parentType});

            var il = method.GetILGenerator();

            var fieldByteArray = il.DeclareLocal(typeof(Byte[]));

            var localBytes = il.DeclareLocal(typeof(Byte[]));

            LocalBuilder localString = null;

            var isArrayMade = false;
            var isPushed = false;

            AddFieldsToPrintMethod(il, ref isArrayMade, ref localString, fieldByteArray,
                ref localBytes, fields, parentType, utfField, ilg => ilg.Emit(OpCodes.Ldarg_1),
                ref isPushed);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, _flush);

            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private void PrintHeaderBytes(Byte[] headerBytes, ILGenerator il,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray,
            Boolean? isPushed)
        {
            if (headerBytes.Length > 1)
            {
                il.Emit(OpCodes.Ldarg_0);

                if (!isArrayMade)
                {
                    il.Emit(OpCodes.Ldc_I4_3);
                    il.Emit(OpCodes.Newarr, typeof(Byte));
                    il.Emit(OpCodes.Stloc, fieldByteArray);
                    isArrayMade = true;
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
                il.Emit(OpCodes.Callvirt, _writeSomeBytes);
            }
            else
            {
                PrintConstByte(headerBytes[0], il, isPushed);
            }
        }

        private void PrintConstByte(Byte constVal, ILGenerator il, Boolean? isPushed)
        {
            var hasStackDepth = il.DefineLabel();
            var noStackDepth = il.DefineLabel();
            var endOfPrintConst = il.DefineLabel();

            switch (isPushed)
            {
                case false:
                    goto notPushed;
                case true:
                    goto yesPushed;
                case null:
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, _stackDepthField);
                    il.Emit(OpCodes.Brtrue, hasStackDepth);

                    il.Emit(OpCodes.Br, noStackDepth);

                    break;
            }

            yesPushed:
            il.MarkLabel(hasStackDepth);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4_S, constVal);
            il.Emit(OpCodes.Call, _unsafeStackByte);

            il.Emit(OpCodes.Br, endOfPrintConst);

            notPushed:
            il.MarkLabel(noStackDepth);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _outStreamField);
            il.Emit(OpCodes.Ldc_I4_S, constVal);
            il.Emit(OpCodes.Callvirt, _writeStreamByte);

            il.MarkLabel(endOfPrintConst);
        }

        private void PrintCollectionProperty(IProtoField pv, ILGenerator il, Byte[] headerBytes,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray, MethodInfo getMethod,
            ref LocalBuilder localBytes,ref LocalBuilder localString, FieldInfo utfField, 
            ref Boolean hasPushed)
        {
            var getEnumeratorMethod = pv.Type.GetMethodOrDie(nameof(IEnumerable.GetEnumerator));
            var enumeratorDisposeMethod = getEnumeratorMethod.ReturnType.GetMethod(
                nameof(IDisposable.Dispose));

            var enumeratorMoveNext = typeof(IEnumerator).GetMethodOrDie(
                nameof(IEnumerator.MoveNext));

            var isExplicit = enumeratorDisposeMethod == null;
            if (isExplicit)
            {
                enumeratorDisposeMethod = typeof(IDisposable).GetMethodOrDie(
                    nameof(IDisposable.Dispose));
            }
            else
            {
                enumeratorMoveNext = getEnumeratorMethod.ReturnType.GetMethodOrDie(
                    nameof(IEnumerator.MoveNext));
            }

            var enumeratorCurrent = GetOrDie(getEnumeratorMethod.ReturnType,
                nameof(IEnumerator.Current));


            var enumeratorLocal = il.DeclareLocal(getEnumeratorMethod.ReturnType);
            var enumeratorType = enumeratorLocal.LocalType ?? throw new InvalidOperationException();
            var enumeratorCurrentValue = il.DeclareLocal(enumeratorCurrent.ReturnType);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, getMethod);
            il.Emit(OpCodes.Callvirt, getEnumeratorMethod);
            il.Emit(OpCodes.Stloc, enumeratorLocal);

            var allDone = il.DefineLabel();

            /////////////////////////////////////
            // TRY
            /////////////////////////////////////
            il.BeginExceptionBlock();
            {
                var tryNext = il.DefineLabel();
                il.MarkLabel(tryNext);

                /////////////////////////////////////
                // !enumerator.HasNext() -> EXIT LOOP
                /////////////////////////////////////
                if (enumeratorType.IsValueType)
                    il.Emit(OpCodes.Ldloca, enumeratorLocal);
                else
                    il.Emit(OpCodes.Ldloc, enumeratorLocal);
                il.Emit(OpCodes.Call, enumeratorMoveNext);
                il.Emit(OpCodes.Brfalse, allDone);


                /////////////////////////////////////
                // PRINT FIELD'S HEADER
                /////////////////////////////////////
                PrintHeaderBytes(headerBytes, il, ref isArrayMade, fieldByteArray, null);

                if (enumeratorType.IsValueType)
                    il.Emit(OpCodes.Ldloca, enumeratorLocal);
                else
                    il.Emit(OpCodes.Ldloc, enumeratorLocal);
                il.Emit(OpCodes.Callvirt, enumeratorCurrent);

                il.Emit(OpCodes.Stloc, enumeratorCurrentValue);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, _push);
                il.Emit(OpCodes.Pop);
                hasPushed = true;
                /////////////

                if (typeof(IDictionary).IsAssignableFrom(pv.Type))
                {
                    var info = new ProtoDictionaryInfo(pv.Type, _types);

                    /////////////////////////////////////
                    // PRINT KEY'S HEADER / KEY'S VALUE
                    /////////////////////////////////////
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, info.KeyHeader);
                    il.Emit(OpCodes.Callvirt, _writeInt32);

                    AddGettableValueToPrintMethod(il, ref isArrayMade, fieldByteArray,
                        ref localBytes,
                        ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
                        ref localString, utfField,
                        Type.GetTypeCode(info.KeyType), info.KeyWireType, info.KeyType, 
                        info.KeyGetter, ref hasPushed);

                    /////////////////////////////////////
                    // PRINT VALUE'S HEADER / VALUE'S VALUE
                    /////////////////////////////////////
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, info.ValueHeader);
                    il.Emit(OpCodes.Callvirt, _writeInt32);

                    AddGettableValueToPrintMethod(il, ref isArrayMade, fieldByteArray,
                        ref localBytes,
                        ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
                        ref localString, utfField,
                        Type.GetTypeCode(info.ValueType), info.ValueWireType, 
                        info.ValueType, info.ValueGetter, ref hasPushed);
                }
                else
                {

                }

                /////////////

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, _pop);
                il.Emit(OpCodes.Pop);

                il.Emit(OpCodes.Br, tryNext);

                il.MarkLabel(allDone);
            }

            /////////////////////////////////////
            // FINALLY
            /////////////////////////////////////
            il.BeginFinallyBlock();
            {
                if (enumeratorType.IsValueType)
                    il.Emit(OpCodes.Ldloca, enumeratorLocal);
                else
                    il.Emit(OpCodes.Ldloc, enumeratorLocal);
                il.Emit(OpCodes.Call, enumeratorDisposeMethod);
            }
            il.EndExceptionBlock();
        }

        private void AddFieldsToPrintMethod(ILGenerator il, ref Boolean isArrayMade,
            ref LocalBuilder localString,
            LocalBuilder fieldByteArray, ref LocalBuilder localBytes,
            IEnumerable<IProtoField> fields, Type parentType, 
            FieldInfo utfField, Action<ILGenerator> loadObject,
            ref Boolean hasPushed)
        {
            foreach (var pv in fields)
            {
                AddFieldToPrintMethod(il, parentType, pv, ref isArrayMade, fieldByteArray,
                    ref localBytes, loadObject, ref localString, utfField, ref hasPushed);
            }
        }

        private void AddFieldToPrintMethod(ILGenerator il, Type parentType, IProtoField pv,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray, ref LocalBuilder localBytes,
            Action<ILGenerator> loadObject, ref LocalBuilder localString, FieldInfo utfField,
            ref Boolean hasPushed)
        {
            var pvProp = parentType.GetProperty(pv.Name) ?? throw new InvalidOperationException();
            var getMethod = pvProp.GetGetMethod();

            var headerBytes = GetBytes(pv.Header).ToArray();

            if (!_types.IsCollection(pv.Type) || pv.Type == Const.ByteArrayType)
            {
                PrintHeaderBytes(headerBytes, il, ref isArrayMade, fieldByteArray, hasPushed);

                AddGettableValueToPrintMethod(il, ref isArrayMade, fieldByteArray, ref localBytes,
                    loadObject, ref localString, utfField, pv.TypeCode, pv.WireType,
                    pv.Type, getMethod, ref hasPushed);
            }
            else
            {
                PrintCollectionProperty(pv, il, headerBytes, ref isArrayMade,
                    fieldByteArray, getMethod, ref localBytes, ref localString, utfField,
                    ref hasPushed);

            }
        }

        private void AddGettableValueToPrintMethod(ILGenerator il,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray, ref LocalBuilder localBytes,
            Action<ILGenerator> loadObject, ref LocalBuilder localString, FieldInfo utfField,
            TypeCode code, ProtoWireTypes wireType, Type type, MethodInfo getMethod, 
            ref Boolean hasPushed)
        {
            il.Emit(OpCodes.Ldarg_0);
            loadObject(il);

            switch (wireType)
            {
                case ProtoWireTypes.Varint:
                case ProtoWireTypes.Int64:
                case ProtoWireTypes.Int32:
                    switch (code)
                    {
                        case TypeCode.Int32:
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Callvirt, _writeInt32);

                            break;
                        case TypeCode.Int64:
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Callvirt, _writeInt64);
                            break;
                        case TypeCode.Single:

                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Call, _getSingleBytes);
                            il.Emit(OpCodes.Callvirt, _writeBytes);

                            break;
                        case TypeCode.Double:
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Call, _getDoubleBytes);
                            il.Emit(OpCodes.Callvirt, _writeBytes);
                            break;
                        case TypeCode.Decimal:
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Call, _getDoubleBytes);
                            il.Emit(OpCodes.Callvirt, _writeBytes);
                            break;

                        case TypeCode.Byte:
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Callvirt, _writeInt8);
                            break;
                        default:
                            throw new NotImplementedException();
                            // if (!Print(pv.Value, code))
                            //     throw new InvalidOperationException();
                            
                    }

                    break;
                case ProtoWireTypes.LengthDelimited:
                    switch (code)
                    {

                        case TypeCode.String:
                            ////////////
                            // STRING
                            ///////////
                            localString = localString ?? il.DeclareLocal(typeof(String));

                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Stloc, localString);

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, utfField);
                            il.Emit(OpCodes.Ldloc, localString);

                            il.Emit(OpCodes.Callvirt, _getStringBytes);
                            il.Emit(OpCodes.Stloc, localBytes);

                            il.Emit(OpCodes.Ldloc, localBytes);
                            il.Emit(OpCodes.Call, _getArrayLength);
                            il.Emit(OpCodes.Call, _writeInt32);

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
                                il.Emit(OpCodes.Call, getMethod);
                                il.Emit(OpCodes.Stloc, localBytes);

                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldloc, localBytes);
                                il.Emit(OpCodes.Call, _getArrayLength);
                                il.Emit(OpCodes.Call, _writeInt32);

                                il.Emit(OpCodes.Ldloc, localBytes);
                                il.Emit(OpCodes.Call, _writeBytes);

                                break;
                            }

                            var localForPropVal = il.DeclareLocal(type);
                            
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Stloc, localForPropVal);

                            var subFields = GetProtoFields(type);

                            hasPushed = true;
                            il.Emit(OpCodes.Callvirt, _push);
                            il.Emit(OpCodes.Pop);

                            AddFieldsToPrintMethod(il, ref isArrayMade, ref localString,
                                fieldByteArray, ref localBytes, subFields, type, utfField,
                                ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal),
                                ref hasPushed);

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, _pop);

                            il.Emit(OpCodes.Pop);


                            break;
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
