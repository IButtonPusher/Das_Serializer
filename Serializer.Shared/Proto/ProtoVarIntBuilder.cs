using System;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
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
                            il.Emit(OpCodes.Ldarg_1);

                            il.Emit(OpCodes.Ldsfld, _readBytesField);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldc_I4_4);
                            il.Emit(OpCodes.Callvirt, ReadStreamBytes);
                            il.Emit(OpCodes.Pop);

                            il.Emit(OpCodes.Ldsfld, _readBytesField);

                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Call, _bytesToSingle);

                            break;

                        /////////////
                        // DOUBLE
                        /////////////
                        case TypeCode.Double:

                            il.Emit(OpCodes.Ldarg_1);


                            il.Emit(OpCodes.Ldsfld, _readBytesField);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Ldc_I4_8);
                            il.Emit(OpCodes.Callvirt, ReadStreamBytes);
                            il.Emit(OpCodes.Pop);

                            il.Emit(OpCodes.Ldsfld, _readBytesField);
                            il.Emit(OpCodes.Ldc_I4_0);
                            il.Emit(OpCodes.Call, _bytesToDouble);

                            return;
                        default:
                            throw new NotImplementedException();
                    }

                    break;
                default:
                    return;
            }
        }
    }
}