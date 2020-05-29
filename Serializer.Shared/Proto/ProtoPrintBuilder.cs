using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            FieldInfo utfField, ICollection<IProtoFieldAccessor> fields)
        {
            var abstractMethod = genericParent.GetMethod(
                                     nameof(ProtoDynamicBase<Object>.Print))
                                 ?? throw new InvalidOperationException();

            var method = bldr.DefineMethod(nameof(ProtoDynamicBase<Object>.Print),
                MethodOverride, typeof(void), new[] {parentType, typeof(Stream)});

            var il = method.GetILGenerator();

            var fieldByteArray = il.DeclareLocal(typeof(Byte[]));

            var localBytes = il.DeclareLocal(typeof(Byte[]));

            LocalBuilder? localString = null;

            var isArrayMade = false;
            var isPushed = false;

            var state = new ProtoPrintState(il, isArrayMade, localString, fieldByteArray,
                localBytes, fields, parentType, utfField, ilg => ilg.Emit(OpCodes.Ldarg_1),
                isPushed, _types);

            ////////////
            
            
            foreach (var s in state)
            {
                //s.CurrentField = pv;

                AddFieldToPrintMethod(s, ilg => ilg.Emit(OpCodes.Ldarg_1));
                //il, parentType, pv, ref isArrayMade, fieldByteArray,
                //ref localBytes, loadObject, ref localString, utfField, ref hasPushed);
            }
            
            //AddFieldsToPrintMethod(state, ilg => ilg.Emit(OpCodes.Ldarg_1));
                //il, ref isArrayMade, ref localString, fieldByteArray,
                //ref localBytes, fields, parentType, utfField, ilg => ilg.Emit(OpCodes.Ldarg_1),
                //ref isPushed);
            ////////////

            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Callvirt, _flush);

            il.Emit(OpCodes.Ret);

            bldr.DefineMethodOverride(method, abstractMethod);
        }

        private void PrintHeaderBytes(Byte[] headerBytes,
            ProtoPrintState s)
            //ILGenerator il,
            //ref Boolean isArrayMade, LocalBuilder fieldByteArray,
            //Boolean? isPushed)
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
                il.Emit(OpCodes.Callvirt, _writeSomeBytes);
            }
            else
            {
                PrintConstByte(headerBytes[0], il, s.HasPushed);
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
            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldc_I4_S, constVal);
            il.Emit(OpCodes.Callvirt, _writeStreamByte);

            il.MarkLabel(endOfPrintConst);
        }

        private void PrintCollectionItem(
            LocalBuilder enumeratorCurrentValue, 
            ProtoPrintState s,
            ILGenerator il,
            //ref Boolean isArrayMade,
            Byte[] headerBytes)
        {
            var shallPush = true;
            var fieldByteArray = s.FieldByteArray;


            PrintHeaderBytes(headerBytes, s);
                //il, ref isArrayMade, fieldByteArray, null);

            if (!TryPrintAsDictionary(s, enumeratorCurrentValue))
                //pv, il, ref isArrayMade,
                //fieldByteArray, ref localBytes, ref localString, utfField,
                //ref hasPushed, enumeratorCurrentValue))
            {
                var info = new ProtoCollectionItem(s.CurrentField.Type, _types, s.CurrentField.Index);
                    //pv.Type, _types, pv.Index);
                shallPush = info.WireType == ProtoWireTypes.LengthDelimited &&
                            Const.StrType != info.Type;

                if (shallPush)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, _push);
                    il.Emit(OpCodes.Pop);
                    s.HasPushed = true;

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, info.Header);
                    il.Emit(OpCodes.Callvirt, _writeInt32);
                }

                AddObtainableValueToPrintMethod(s,
                    ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
                //il, ref isArrayMade, fieldByteArray,
                //ref localBytes,
                //ref localString, utfField,
                //info.TypeCode, info.WireType, info.Type,
                //ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue),
                //ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
                //ref hasPushed);
            }

            /////////////

            if (shallPush)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, _pop);
                il.Emit(OpCodes.Pop);
            }
        }
       

        private void PrintCollectionProperty(ProtoPrintState s, MethodInfo getMethod,
                Byte[] headerBytes)
            //IProtoField pv, ILGenerator ilg, Byte[] headerBytes,
            //ref Boolean isArrayMade, LocalBuilder fieldByteArray, MethodInfo getMethod,
            //ref LocalBuilder? localBytes, ref LocalBuilder localString, FieldInfo utfField,
            //ref Boolean hasPushed)
        {
            if (TryPrintAsArray(s, headerBytes))
                //pv, ilg, headerBytes, ref isArrayMade, fieldByteArray,
                //getMethod, ref localBytes, ref localString, utfField, ref hasPushed))
            {
                return;
            }

            //var s = new ProtoBuildState(ilg,);
            
            var ienum = new ProtoEnumerator<ProtoPrintState>(s, s.CurrentField.Type, getMethod);
            var shallPush = true;

            ienum.ForEach(PrintCollectionItem, headerBytes);

            //var getEnumeratorMethod = pv.Type.GetMethodOrDie(nameof(IEnumerable.GetEnumerator));
            //var enumeratorDisposeMethod = getEnumeratorMethod.ReturnType.GetMethod(
            //    nameof(IDisposable.Dispose));

            //var enumeratorMoveNext = typeof(IEnumerator).GetMethodOrDie(
            //    nameof(IEnumerator.MoveNext));

            //var isExplicit = enumeratorDisposeMethod == null;
            //if (isExplicit && typeof(IDisposable).IsAssignableFrom(getEnumeratorMethod.ReturnType))
            //{
            //    enumeratorDisposeMethod = typeof(IDisposable).GetMethodOrDie(
            //        nameof(IDisposable.Dispose));
            //}
            //else
            //{
            //    enumeratorMoveNext = getEnumeratorMethod.ReturnType.GetMethodOrDie(
            //        nameof(IEnumerator.MoveNext));
            //}

            //var enumeratorCurrent = getEnumeratorMethod.ReturnType.GetterOrDie(
            //    nameof(IEnumerator.Current), out _);


            //var enumeratorLocal = il.DeclareLocal(getEnumeratorMethod.ReturnType);
            //var enumeratorType = enumeratorLocal.LocalType ?? throw new InvalidOperationException();
            //var enumeratorCurrentValue = il.DeclareLocal(enumeratorCurrent.ReturnType);

            //il.Emit(OpCodes.Ldarg_1);
            //il.Emit(OpCodes.Call, getMethod);
            //il.Emit(OpCodes.Callvirt, getEnumeratorMethod);
            //il.Emit(OpCodes.Stloc, enumeratorLocal);

            //var allDone = il.DefineLabel();

            ///////////////////////////////////////
            //// TRY
            ///////////////////////////////////////
            //if (enumeratorDisposeMethod != null)
            //    il.BeginExceptionBlock();
            //{
            //    var tryNext = il.DefineLabel();
            //    il.MarkLabel(tryNext);

            //    /////////////////////////////////////
            //    // !enumerator.HasNext() -> EXIT LOOP
            //    /////////////////////////////////////
            //    if (enumeratorType.IsValueType)
            //        il.Emit(OpCodes.Ldloca, enumeratorLocal);
            //    else
            //        il.Emit(OpCodes.Ldloc, enumeratorLocal);
            //    il.Emit(OpCodes.Call, enumeratorMoveNext);
            //    il.Emit(OpCodes.Brfalse, allDone);


            //    /////////////////////////////////////
            //    // PRINT FIELD'S HEADER
            //    /////////////////////////////////////
            //    PrintHeaderBytes(headerBytes, il, ref isArrayMade, fieldByteArray, null);

            //    if (enumeratorType.IsValueType)
            //        il.Emit(OpCodes.Ldloca, enumeratorLocal);
            //    else
            //        il.Emit(OpCodes.Ldloc, enumeratorLocal);
            //    il.Emit(OpCodes.Callvirt, enumeratorCurrent);

            //    il.Emit(OpCodes.Stloc, enumeratorCurrentValue);

            //    /////////////

            //    var shallPush = true;


            //    if (!TryPrintAsDictionary(pv, il, ref isArrayMade,
            //        fieldByteArray, ref localBytes, ref localString, utfField,
            //        ref hasPushed, enumeratorCurrentValue))
            //    {
            //        var info = new ProtoCollectionItem(pv.Type, _types, pv.Index);
            //        shallPush = info.WireType == ProtoWireTypes.LengthDelimited &&
            //                    Const.StrType != info.Type;

            //        if (shallPush)
            //        {
            //            il.Emit(OpCodes.Ldarg_0);
            //            il.Emit(OpCodes.Callvirt, _push);
            //            il.Emit(OpCodes.Pop);
            //            hasPushed = true;

            //            il.Emit(OpCodes.Ldarg_0);
            //            il.Emit(OpCodes.Ldc_I4, info.Header);
            //            il.Emit(OpCodes.Callvirt, _writeInt32);
            //        }

            //        AddObtainableValueToPrintMethod(il, ref isArrayMade, fieldByteArray,
            //            ref localBytes,
            //            ref localString, utfField,
            //            info.TypeCode, info.WireType, info.Type,
            //            ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue),
            //            ilg => ilg.Emit(OpCodes.Ldloca, enumeratorCurrentValue),
            //            ref hasPushed);
            //    }

            //    /////////////

            //    if (shallPush)
            //    {
            //        il.Emit(OpCodes.Ldarg_0);
            //        il.Emit(OpCodes.Callvirt, _pop);
            //        il.Emit(OpCodes.Pop);
            //    }

            //    il.Emit(OpCodes.Br, tryNext);

            //    il.MarkLabel(allDone);
            //}

            //if (enumeratorDisposeMethod == null)
            //    return;

            ///////////////////////////////////////
            //// FINALLY
            ///////////////////////////////////////
            //il.BeginFinallyBlock();
            //{
            //    if (enumeratorType.IsValueType)
            //        il.Emit(OpCodes.Ldloca, enumeratorLocal);
            //    else
            //        il.Emit(OpCodes.Ldloc, enumeratorLocal);
            //    il.Emit(OpCodes.Call, enumeratorDisposeMethod);
            //}
            //il.EndExceptionBlock();
        }

        //private void AddFieldsToPrintMethod(ProtoPrintState s,
        //        Action<ILGenerator> loadObject)
        //    //ILGenerator il, ref Boolean isArrayMade,
        //    //ref LocalBuilder? localString,
        //    //LocalBuilder fieldByteArray, ref LocalBuilder? localBytes,
        //    //IEnumerable<IProtoField> fields, Type parentType,
        //    //FieldInfo utfField, Action<ILGenerator> loadObject,
        //    //ref Boolean hasPushed)
        //{
            

        //    foreach (var pv in s.Fields)
        //    {
        //        s.CurrentField = pv;

        //        AddFieldToPrintMethod(s, loadObject);
        //        //il, parentType, pv, ref isArrayMade, fieldByteArray,
        //        //ref localBytes, loadObject, ref localString, utfField, ref hasPushed);
        //    }
        //}

        private void AddFieldToPrintMethod(ProtoPrintState s, 
                Action<ILGenerator> loadObject)
            //ILGenerator il, Type parentType, IProtoField pv,
            //ref Boolean isArrayMade, LocalBuilder fieldByteArray, ref LocalBuilder? localBytes,
            //Action<ILGenerator> loadObject, ref LocalBuilder? localString, FieldInfo utfField,
            //ref Boolean hasPushed)
        {
            var pv = s.CurrentField;

            var ifFalse2 = VerifyValueIsNonDefault(s, pv.GetMethod);

            switch (pv.FieldAction)
            {
                case ProtoFieldAction.Primitive:
                    case ProtoFieldAction.VarInt:
                    PrintPrimitive(s);
                    break;

                case ProtoFieldAction.String:
                    PrintString(s);
                    break;

                case ProtoFieldAction.ByteArray:
                    PrintByteArray(s);
                    break;

                case ProtoFieldAction.PackedArray:
                    TryPrintAsPackedArray(s);
                    break;

                case ProtoFieldAction.ChildObject:
                    PrintChildObject(s, s.CurrentField.HeaderBytes,
                        _ => s.LoadCurrentFieldValueToStack());
                    break;

                case ProtoFieldAction.ChildObjectCollection:
                    PrintCollection(s);
                    break;

                case ProtoFieldAction.Dictionary:
                    PrintDictionary(s);
                    break;

                case ProtoFieldAction.ChildObjectArray:
                    TryPrintAsArray(s, pv.HeaderBytes);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            s.IL.MarkLabel(ifFalse2);

            return;


            var pvProp = s.ParentType.GetProperty(pv.Name) ?? throw new InvalidOperationException();
            var getMethod = pvProp.GetGetMethod();

            var headerBytes = GetBytes(pv.Header).ToArray();

            // primitive or packed array
            if (!_types.IsCollection(pv.Type) ||
                typeof(IEnumerable<Byte>).IsAssignableFrom(pv.Type) ||
                GetPackedArrayType(pv.Type) != null)
            {
                var ifFalse = VerifyValueIsNonDefault(s, getMethod);

                PrintHeaderBytes(headerBytes, s);
                    //il, ref isArrayMade, fieldByteArray, hasPushed);

                    AddGettableValueToPrintMethod(s, getMethod, loadObject);
                    //il, ref isArrayMade, fieldByteArray, ref localBytes,
                    //loadObject, ref localString, utfField, pv.TypeCode, pv.WireType,
                    //pv.Type, getMethod, ref hasPushed);

                s.IL.MarkLabel(ifFalse);
            }

            else
            {
                PrintCollectionProperty(s, getMethod, headerBytes);
                //pv, il, headerBytes, ref isArrayMade,
                //fieldByteArray, getMethod, ref localBytes, ref localString, utfField,
                //ref hasPushed);
            }
        }

        private static Label VerifyValueIsNonDefault(ProtoPrintState s,
            MethodInfo propertyGetter)
            //ILGenerator il,
            //MethodInfo propertyGetter,
            //Type propertyType)
        {
            var il = s.IL;
            var propertyType = s.CurrentField.Type;

            var gotoIfFalse = il.DefineLabel();

            if (propertyType.IsPrimitive)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, propertyGetter);

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

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, propertyGetter);
            il.Emit(OpCodes.Callvirt, countGetter);
            il.Emit(OpCodes.Brfalse, gotoIfFalse);

            done:
            return gotoIfFalse;
        }

        private void AddGettableValueToPrintMethod(ProtoPrintState s, MethodInfo getMethod,
                Action<ILGenerator> loadObject)
            //ILGenerator il,
            //ref Boolean isArrayMade, LocalBuilder fieldByteArray, ref LocalBuilder? localBytes,
            //Action<ILGenerator> loadObject, ref LocalBuilder? localString, FieldInfo utfField,
            //TypeCode code, ProtoWireTypes wireType, Type type, MethodInfo getMethod,
            //ref Boolean hasPushed)
        {
            var il = s.IL;
            var wireType = s.CurrentField.WireType;
            var code = s.CurrentField.TypeCode;
            var type = s.CurrentField.Type;


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

                        case TypeCode.Int16:
                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Callvirt, _writeInt16);
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
                            s.LocalString ??= il.DeclareLocal(typeof(String));

                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Stloc, s.LocalString);

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, s.UtfField);
                            il.Emit(OpCodes.Ldloc, s.LocalString);

                            il.Emit(OpCodes.Callvirt, _getStringBytes);
                            il.Emit(OpCodes.Stloc, s.LocalBytes);

                            il.Emit(OpCodes.Ldloc, s.LocalBytes);
                            il.Emit(OpCodes.Call, _getArrayLength);
                            il.Emit(OpCodes.Call, _writeInt32);

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldloc, s.LocalBytes);
                            il.Emit(OpCodes.Call, _writeBytes);

                            break;
                        case TypeCode.Object:

                            ///////////
                            // BYTE [ARRAY]
                            ///////////
                            if (type == Const.ByteArrayType)
                            {
                                il.Emit(OpCodes.Call, getMethod);
                                il.Emit(OpCodes.Stloc, s.LocalBytes);

                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldloc, s.LocalBytes);
                                il.Emit(OpCodes.Call, _getArrayLength);
                                il.Emit(OpCodes.Call, _writeInt32);

                                il.Emit(OpCodes.Ldloc, s.LocalBytes);
                                il.Emit(OpCodes.Call, _writeBytes);

                                break;
                            }

                            //////////////////////
                            // PACKED REPEATED
                            //////////////////////
                            //else if (typeof(IEnumerable<Int32>).IsAssignableFrom(type) ||
                            //         typeof(IEnumerable<Int16>).IsAssignableFrom(type) ||
                            //         typeof(IEnumerable<Int32>).IsAssignableFrom(type))

                            else if (TryPrintAsPackedArray(s))
                                break;
                            //else if (GetPackedArrayType(type) is {} packType)
                            //{
                            //    // var ienum = obj.Property;
                            //    var ienum = il.DeclareLocal(type);

                            //    il.Emit(OpCodes.Call, getMethod);
                            //    il.Emit(OpCodes.Stloc, ienum);

                            //    // WriteInt32(GetPackedArrayLength(ienum)); 
                            //    il.Emit(OpCodes.Ldarg_0);
                            //    il.Emit(OpCodes.Ldloc, ienum);

                            //    if (packType == typeof(Int32))
                            //    {
                            //        var methos = _getPackedInt32Length.MakeGenericMethod(type);
                            //        il.Emit(OpCodes.Call, methos);
                            //    }

                            //    if (packType == typeof(Int16))
                            //    {
                            //        var methos = _getPackedInt16Length.MakeGenericMethod(type);
                            //        il.Emit(OpCodes.Call, methos);
                            //    }

                            //    if (packType == typeof(Int64))
                            //    {
                            //        var methos = _getPackedInt64Length.MakeGenericMethod(type);
                            //        il.Emit(OpCodes.Call, methos);
                            //    }



                            //    il.Emit(OpCodes.Call, _writeInt32);

                            //    // WritePacked(ienum);
                            //    il.Emit(OpCodes.Ldarg_0);
                            //    il.Emit(OpCodes.Ldloc, ienum);

                            //    if (typeof(IEnumerable<Int32>).IsAssignableFrom(type))
                            //    {
                            //        var methos = _writePacked32.MakeGenericMethod(type);
                            //        il.Emit(OpCodes.Call, methos);
                            //    }

                            //    else if (typeof(IEnumerable<Int16>).IsAssignableFrom(type))
                            //    {
                            //        var methos = _writePacked16.MakeGenericMethod(type);
                            //        il.Emit(OpCodes.Call, methos);
                            //    }

                            //    else if (typeof(IEnumerable<Int64>).IsAssignableFrom(type))
                            //    {
                            //        var methos = _writePacked64.MakeGenericMethod(type);
                            //        il.Emit(OpCodes.Call, methos);
                            //    }

                            //    break;
                            //}

                            var localForPropVal = il.DeclareLocal(type);

                            il.Emit(OpCodes.Call, getMethod);
                            il.Emit(OpCodes.Stloc, localForPropVal);

                            var subFields = GetProtoFields(type);

                            s.HasPushed = true;
                            il.Emit(OpCodes.Callvirt, _push);
                            il.Emit(OpCodes.Pop);

                            var state = new ProtoPrintState(s, subFields, type, 
                                ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal),
                                 _types);

                            foreach (var sub in state)
                            {
                                AddFieldToPrintMethod(sub, ilg => ilg.Emit(OpCodes.Ldarg_1));
                                s.MergeLocals(sub);
                            }


                            //AddFieldsToPrintMethod(s, ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal));
                            //    //il, ref isArrayMade, ref localString,
                            //    //fieldByteArray, ref localBytes, subFields, type, utfField,
                            //    //ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal),
                            //    //ref hasPushed);

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

        private void PrintChildObject(ProtoPrintState s,
            Byte[] headerBytes,
            Action<ILGenerator> loadObject)
        {
            var il = s.IL;

            //PrintHeaderBytes(s.CurrentField.HeaderBytes, s);
            PrintHeaderBytes(headerBytes, s);

            var proxyLocal = s.ChildProxies[s.CurrentField];
            var proxyType = proxyLocal.LocalType;


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

            il.Emit(OpCodes.Ldloc, proxyLocal);
            loadObject(il);

            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);

            //il.Emit(OpCodes.Ldarg_2);
            
            //il.Emit(OpCodes.Ldloc, enumeratorCurrentValue);
            il.Emit(OpCodes.Call, printMethod);


            ////////////////////////////////////////////
            // PRINT LENGTH OF CHILD STREAM
            ////////////////////////////////////////////
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Callvirt, _getStreamLength);
            il.Emit(OpCodes.Callvirt, _writeInt64);

            ////////////////////////////////////////////
            // COPY CHILD STREAM TO MAIN
            ////////////////////////////////////////////
            //reset stream
            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Callvirt, _setStreamPosition);
            
            
            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, _copyStreamTo);
            

            il.Emit(OpCodes.Ldloc, s.ChildObjectStream);
            il.Emit(OpCodes.Ldc_I8, 0L);
            il.Emit(OpCodes.Callvirt, _setStreamLength);

            //var il = s.IL;
            //var type = s.CurrentField.Type;

            //var localForPropVal = il.DeclareLocal(type);

            //il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
            //il.Emit(OpCodes.Stloc, localForPropVal);

            //var subFields = GetProtoFields(type);

            //s.HasPushed = true;
            //il.Emit(OpCodes.Callvirt, _push);
            //il.Emit(OpCodes.Pop);

            //var state = new ProtoPrintState(s, subFields, type, 
            //    ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal), _types);

            //foreach (var sub in state)
            //{
            //    AddFieldToPrintMethod(sub, ilg => ilg.Emit(OpCodes.Ldarg_1));
            //    s.MergeLocals(sub);
            //}

            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Callvirt, _pop);

            //il.Emit(OpCodes.Pop);
        }


        private void AddObtainableValueToPrintMethod(ProtoPrintState s, 
            Action<ILGenerator> loadValue)
            //ILGenerator il,
            //ref Boolean isArrayMade, LocalBuilder fieldByteArray,
            //ref LocalBuilder? localBytes,
            //ref LocalBuilder localString, FieldInfo utfField,
            //TypeCode code, ProtoWireTypes wireType, Type type,
            //Action<ILGenerator> loadValue, Action<ILGenerator> loadObject,
            //ref Boolean hasPushed)
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

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldfld, s.UtfField);
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
                                loadValue(il);
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

                            loadValue(il);
                            il.Emit(OpCodes.Stloc, localForPropVal);

                            var subFields = GetProtoFields(type);

                            s.HasPushed = true;
                            il.Emit(OpCodes.Callvirt, _push);
                            il.Emit(OpCodes.Pop);

                            var state = new ProtoPrintState(s, subFields, type, 
                                ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal), _types);

                            foreach (var sub in state)
                            {
                                AddFieldToPrintMethod(sub, ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal));
                                s.MergeLocals(sub);
                            }

                            //AddFieldsToPrintMethod(s, ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal));
                            //    //il, ref isArrayMade, ref localString,
                            ////    fieldByteArray, ref localBytes, subFields, type, utfField,
                            ////    ilg => ilg.Emit(OpCodes.Ldloc, localForPropVal),
                            ////    ref hasPushed);

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
