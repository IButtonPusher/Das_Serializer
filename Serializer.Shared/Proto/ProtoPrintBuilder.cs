using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
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


            var doubleBytes = il.DeclareLocal(typeof(Double));
            var singleBytes = il.DeclareLocal(typeof(Single));

            var localBytes = il.DeclareLocal(typeof(Byte[]));

            LocalBuilder localString = null;

            var isArrayMade = false;

            AddFieldsToPrintMethod(il, ref isArrayMade, ref localString, fieldByteArray,
                ref localBytes, fields, parentType, utfField, ilg => ilg.Emit(OpCodes.Ldarg_1));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, _flush);

            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private void PrintHeaderBytes(Byte[] headerBytes, ILGenerator il,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray)
        {
            il.Emit(OpCodes.Ldarg_0);

            if (headerBytes.Length > 1)
            {
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
                il.Emit(OpCodes.Ldc_I4_S, headerBytes[0]);
                il.Emit(OpCodes.Callvirt, _writeInt8);
            }
        }

        private void PrintCollectionProperty(IProtoField pv, ILGenerator il,
            Action<ILGenerator> loadObject, Byte[] headerBytes,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray, MethodInfo getMethod,
            ref LocalBuilder localBytes,ref LocalBuilder localString, FieldInfo utfField)
        {
            var getEnumeratorMethod = GetMethodOrDie(pv.Type, nameof(IEnumerable.GetEnumerator));
            var enumeratorDisposeMethod = getEnumeratorMethod.ReturnType.GetMethod(nameof(IDisposable.Dispose));

            var enumeratorMoveNext = GetMethodOrDie(typeof(IEnumerator),
                nameof(IEnumerator.MoveNext));

            var isExplicit = enumeratorDisposeMethod == null;
            if (isExplicit)
            {
                enumeratorDisposeMethod = GetMethodOrDie(typeof(IDisposable),
                    nameof(IDisposable.Dispose));
            }
            else
            {
                enumeratorMoveNext = getEnumeratorMethod.ReturnType.GetMethod(
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
                PrintHeaderBytes(headerBytes, il, ref isArrayMade, fieldByteArray);

                if (enumeratorType.IsValueType)
                    il.Emit(OpCodes.Ldloca, enumeratorLocal);
                else
                    il.Emit(OpCodes.Ldloc, enumeratorLocal);
                il.Emit(OpCodes.Callvirt, enumeratorCurrent);

                il.Emit(OpCodes.Stloc, enumeratorCurrentValue);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, _push);
                il.Emit(OpCodes.Pop);
                /////////////

                if (typeof(IDictionary).IsAssignableFrom(pv.Type))
                {
                    var gargs = pv.Type.GetGenericArguments();
                    var keyType = gargs[0];
                    var valueType = gargs[1];

                    var keyWireType = ProtoStructure.GetWireType(keyType);
                    var keyHeader = (Int32) keyWireType + (1 << 3);

                    var keyGetter = GetOrDie(enumeratorCurrentValue.LocalType,
                        nameof(KeyValuePair<Object, Object>.Key));


                    var valueWireType = ProtoStructure.GetWireType(valueType);
                    var valueHeader = (Int32) valueWireType + (1 << 3);

                    var valueGetter = GetOrDie(enumeratorCurrentValue.LocalType,
                        nameof(KeyValuePair<Object, Object>.Value));

                    /////////////////////////////////////
                    // PRINT KEY'S HEADER / KEY'S VALUE
                    /////////////////////////////////////
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, keyHeader);
                    il.Emit(OpCodes.Callvirt, _writeInt32);

                    AddGettableValueToPrintMethod(il, ref isArrayMade, fieldByteArray,
                        ref localBytes,
                        ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
                        ref localString, utfField,
                        Type.GetTypeCode(keyType), keyWireType, keyType, keyGetter);

                    /////////////////////////////////////
                    // PRINT VALUE'S HEADER / VALUE'S VALUE
                    /////////////////////////////////////
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, valueHeader);
                    il.Emit(OpCodes.Callvirt, _writeInt32);

                    AddGettableValueToPrintMethod(il, ref isArrayMade, fieldByteArray,
                        ref localBytes,
                        ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
                        ref localString, utfField,
                        Type.GetTypeCode(valueType), valueWireType, valueType, valueGetter);


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
            IEnumerable<IProtoField> fields,
            Type parentType, FieldInfo utfField, Action<ILGenerator> loadObject)
        {
            foreach (var pv in fields)
            {
                AddFieldToPrintMethod(il, parentType, pv, ref isArrayMade, fieldByteArray,
                    ref localBytes, loadObject, ref localString, utfField);
            }
        }

        private void AddFieldToPrintMethod(ILGenerator il, Type parentType, IProtoField pv,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray, ref LocalBuilder localBytes,
            Action<ILGenerator> loadObject, ref LocalBuilder localString, FieldInfo utfField)
        {
            var pvProp = parentType.GetProperty(pv.Name) ?? throw new InvalidOperationException();
            var getMethod = pvProp.GetGetMethod();

            var headerBytes = GetBytes(pv.Header).ToArray();

            if (!_types.IsCollection(pv.Type) || pv.Type == Const.ByteArrayType)
            {
                PrintHeaderBytes(headerBytes, il, ref isArrayMade, fieldByteArray);

                AddGettableValueToPrintMethod(il, ref isArrayMade, fieldByteArray, ref localBytes,
                    loadObject, ref localString, utfField, pv.TypeCode, pv.WireType,
                    pv.Type, getMethod);
            }
            else
            {
                PrintCollectionProperty(pv, il, loadObject, headerBytes, ref isArrayMade,
                    fieldByteArray, getMethod, ref localBytes, ref localString, utfField);

            }
        }

        private void AddGettableValueToPrintMethod(ILGenerator il,
            ref Boolean isArrayMade, LocalBuilder fieldByteArray, ref LocalBuilder localBytes,
            Action<ILGenerator> loadObject, ref LocalBuilder localString, FieldInfo utfField,
            TypeCode code, ProtoWireTypes wireType, Type type, MethodInfo getMethod)
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
                            break;
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


                            //bytes = _utf8.GetBytes(s);
                            il.Emit(OpCodes.Callvirt, _getStringBytes);
                            il.Emit(OpCodes.Stloc, localBytes);

                            //WriteInt32(bytes.Length);
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
                            //
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Stloc, localForPropVal);

                            //
                            var subFields = GetProtoFields(type);

                            il.Emit(OpCodes.Callvirt, _push);
                            il.Emit(OpCodes.Pop);

                            AddFieldsToPrintMethod(il, ref isArrayMade, ref localString,
                                fieldByteArray, ref localBytes, subFields, type, utfField,
                                ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal));

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, _pop);

                            il.Emit(OpCodes.Pop);


                            //nested object - have to stack bytes till we know the
                            //total length
                            // properyValues = properyValues.Push();
                            // if (!repeated)
                            //     _bWriter = _writer.Push();

                            break;
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
