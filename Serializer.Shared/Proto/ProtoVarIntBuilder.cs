using System;
using System.Reflection.Emit;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {

        private Boolean TryScanAsVarInt(ProtoScanState s,
                ref LocalBuilder holdForSet)
            
            //ILGenerator il,
            //IProtoField currentProp,LocalBuilder fieldByteArray,
            //LocalBuilder lastByte, ref LocalBuilder holdForSet,
            //Action<ILGenerator> loadObject, 
            //Action<ILGenerator>? setValue,
            //Type parentType, 
            //ProtoArrayInfo arrayCounters,
            //Object exampleObject)
        {
            var currentProp = s.CurrentField;
            var il = s.IL;

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
                        case TypeCode.Boolean:
                            
                            if (s.SetCurrentValue != null)
                                s.LoadCurrentValueOntoStack(il);
                            else
                                holdForSet = holdForSet ?? il.DeclareLocal(typeof(Int32));

                            il.Emit(OpCodes.Ldarg_1);

                            il.Emit(OpCodes.Call, _getInt32);
                            
                            if (s.SetCurrentValue != null)
                                s.SetCurrentValue(il);
                            else
                                il.Emit(OpCodes.Stloc, holdForSet);
                            break;

                        ///////////////
                        // INT16
                        ///////////////
                        case TypeCode.Int16:
                            //holdForSet = holdForSet ?? il.DeclareLocal(typeof(Int16));

                            if (s.SetCurrentValue != null)
                                s.LoadCurrentValueOntoStack(il);
                            else
                                holdForSet = holdForSet ?? il.DeclareLocal(typeof(Int16));

                            il.Emit(OpCodes.Ldarg_1);

                            il.Emit(OpCodes.Call, _getInt32);
                            il.Emit(OpCodes.Conv_I4);

                            if (s.SetCurrentValue != null)
                                s.SetCurrentValue(il);
                            else
                                il.Emit(OpCodes.Stloc, holdForSet);

                            //il.Emit(OpCodes.Stloc, holdForSet);
                            break;


                        ///////////////
                        // SINGLE BYTE
                        ///////////////
                        case TypeCode.Byte:
                            il.Emit(OpCodes.Ldarg_1);

                            holdForSet = holdForSet ?? il.DeclareLocal(typeof(Byte));
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

                    return true;

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

                            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
                            il.Emit(OpCodes.Ldsfld, _readBytes);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldc_I4_4);
                            il.Emit(OpCodes.Callvirt, _readStreamBytes);
                            il.Emit(OpCodes.Pop);

                            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
                            il.Emit(OpCodes.Ldsfld, _readBytes);

                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Call, _bytesToSingle);
                            il.Emit(OpCodes.Stloc, holdForSet);
                            break;

                        /////////////
                        // DOUBLE
                        /////////////
                        case TypeCode.Double:

                            if (s.SetCurrentValue != null)
                                s.LoadCurrentValueOntoStack(il);
                            else
                                holdForSet = holdForSet ?? il.DeclareLocal(typeof(Int16));

                            //arg_1 = stream
                            il.Emit(OpCodes.Ldarg_1);

                            //holdForSet = holdForSet ?? il.DeclareLocal(typeof(Double));

                            //read bytes from stream into local array
                            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
                            il.Emit(OpCodes.Ldsfld, _readBytes);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldc_I4_8);
                            il.Emit(OpCodes.Callvirt, _readStreamBytes);
                            il.Emit(OpCodes.Pop);

                            //il.Emit(OpCodes.Ldloc, holdForSet);

                            //output.Property = BitConverter.ToDouble(fieldByteArray);
                            //if (s.SetCurrentValue != null)
                            //    s.LoadCurrentValueOntoStack(il);
                            //else
                            //    holdForSet = holdForSet ?? il.DeclareLocal(typeof(Double));

                            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
                            il.Emit(OpCodes.Ldsfld, _readBytes);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Call, _bytesToDouble);

                            if (s.SetCurrentValue != null)
                                s.SetCurrentValue(il);
                            else
                                il.Emit(OpCodes.Stloc, holdForSet);
                            //il.Emit(OpCodes.Callvirt, setter);


                            return true;
                        default:
                            throw new NotImplementedException();
                    }

                    break;
                default:
                    return false;
            }

            return true;
        }

        private Boolean TryPrintAsVarInt(ProtoPrintState s, Action<ILGenerator> loadValue)
            //ILGenerator il,
            //ref Boolean isArrayMade, LocalBuilder fieldByteArray,
            //ref LocalBuilder? localBytes,
            //ref LocalBuilder localString, FieldInfo utfField,
            //TypeCode code, ProtoWireTypes wireType, Type type,
            //Action<ILGenerator> loadValue, Action<ILGenerator> loadObject,
            //ref Boolean hasPushed)
        {
            var il = s.IL;

            switch (s.CurrentField.WireType)
            {
                case ProtoWireTypes.Varint:
                case ProtoWireTypes.Int64:
                case ProtoWireTypes.Int32:
                    switch (s.CurrentField.TypeCode)
                    {
                        case TypeCode.Int32:
                            loadValue(il);
                            il.Emit(OpCodes.Call, _writeInt32);

                            break;
                        case TypeCode.Int64:
                            loadValue(il);
                            il.Emit(OpCodes.Call, _writeInt64);
                            break;
                        case TypeCode.Single:

                            loadValue(il);
                            il.Emit(OpCodes.Call, _getSingleBytes);
                            il.Emit(OpCodes.Call, _writeBytes);

                            break;
                        case TypeCode.Double:
                            loadValue(il);
                            il.Emit(OpCodes.Call, _getDoubleBytes);
                            il.Emit(OpCodes.Call, _writeBytes);
                            break;

                        case TypeCode.Int16:
                            loadValue(il);
                            il.Emit(OpCodes.Conv_I4);
                            il.Emit(OpCodes.Call, _writeInt32);
                            break;

                        case TypeCode.Decimal:
                            loadValue(il);
                            il.Emit(OpCodes.Call, _getDoubleBytes);
                            il.Emit(OpCodes.Call, _writeBytes);
                            break;

                        case TypeCode.Byte:
                            loadValue(il);
                            il.Emit(OpCodes.Call, _writeInt8);
                            break;
                        default:
                            throw new NotImplementedException();
                        // if (!Print(pv.Value, code))
                        //     throw new InvalidOperationException();

                    }

                    return true;

            }

            return false;
        }
    }
}
