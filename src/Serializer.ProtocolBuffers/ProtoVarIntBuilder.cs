﻿#if GENERATECODE

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void ReadFloatingPoint(ILGenerator il,
                                       Action<ILGenerator> loadLength,
                                       MethodInfo converter)
        {
            il.Emit(OpCodes.Ldarg_1);

            il.Emit(OpCodes.Ldsfld, _readBytesField);
            il.Emit(OpCodes.Ldc_I4_0);
            loadLength(il);
            
            il.Emit(OpCodes.Callvirt, ReadStreamBytes);
            il.Emit(OpCodes.Pop);

            il.Emit(OpCodes.Ldsfld, _readBytesField);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, converter);
        }

        private void ScanAsVarInt(ILGenerator il,
                                  TypeCode typeCode,
                                  ProtoWireTypes wireType)
        {
            switch (wireType)
            {
                /////////////
                // VARINT
                /////////////
                case Const.VarInt:
                    switch (typeCode)
                    {
                        ///////////////
                        // INT32
                        ///////////////
                        case TypeCode.Int32:
                        case TypeCode.Boolean:

                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, _getInt32);
                            break;

                        ///////////////
                        // INT16
                        ///////////////
                        case TypeCode.Int16:
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, _getInt32);
                            il.Emit(OpCodes.Conv_I4);
                            break;

                        ///////////////
                        // SINGLE BYTE
                        ///////////////
                        case TypeCode.Byte:
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Callvirt, _readStreamByte);
                            break;

                        ///////////////
                        // INT64
                        ///////////////
                        case TypeCode.Int64:
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, _getInt64);
                            break;

                        ///////////////
                        // UINT32
                        ///////////////
                        case TypeCode.UInt32:
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, _getUInt32);
                            break;


                        ///////////////
                        // UINT64
                        ///////////////
                        case TypeCode.UInt64:
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Call, _getUInt64);
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    break;

                case ProtoWireTypes.Int64: //64-bit zb double
                case ProtoWireTypes.Int32:

                    //single, double
                    switch (typeCode)
                    {
                        /////////////
                        // SINGLE
                        /////////////
                        case TypeCode.Single:
                            ReadFloatingPoint(il, i => i.Emit(OpCodes.Ldc_I4_4),
                                _bytesToSingle);

                            return;

                        /////////////
                        // DOUBLE
                        /////////////
                        case TypeCode.Double:

                            ReadFloatingPoint(il, i => i.Emit(OpCodes.Ldc_I4_8),
                                _bytesToDouble);

                            return;

                        default:
                            throw new NotImplementedException();
                    }

                    
                    case ProtoWireTypes.LengthDelimited:

                        switch (typeCode)
                        {
                            case TypeCode.Decimal:
                                ReadFloatingPoint(il, i => i.Emit(OpCodes.Ldc_I4, 16),
                                    _bytesToDecimal);
                                return;

                        }

                        break;

                default:
                    return;
            }
        }
    }
}

#endif
