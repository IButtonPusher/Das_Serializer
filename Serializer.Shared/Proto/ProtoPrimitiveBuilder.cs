using System;
using System.Reflection.Emit;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void PrintPrimitive(ProtoPrintState s)
        {
            var pv = s.CurrentField;

            var code = pv.TypeCode;
            var il = s.IL;

            PrintHeaderBytes(pv.HeaderBytes, s);
            
            s.LoadProxyToStack();
            s.LoadCurrentFieldValueToStack();

            switch (code)
            {
                case TypeCode.Int32:
                    il.Emit(OpCodes.Callvirt, _writeInt32);
                    break;

                case TypeCode.Int64:
                    il.Emit(OpCodes.Callvirt, _writeInt64);
                    break;

                case TypeCode.Int16:
                    il.Emit(OpCodes.Callvirt, _writeInt16);
                    break;

                case TypeCode.Single:
                    il.Emit(OpCodes.Call, _getSingleBytes);
                    il.Emit(OpCodes.Callvirt, _writeBytes);
                    break;

                case TypeCode.Double:
                    il.Emit(OpCodes.Call, _getDoubleBytes);
                    il.Emit(OpCodes.Callvirt, _writeBytes);
                    break;

                case TypeCode.Decimal:
                    il.Emit(OpCodes.Call, _getDoubleBytes);
                    il.Emit(OpCodes.Callvirt, _writeBytes);
                    break;

                case TypeCode.Byte:
                    il.Emit(OpCodes.Callvirt, _writeInt8);
                    break;

                    case TypeCode.Boolean:
                        il.Emit(OpCodes.Callvirt, _writeInt32);
                        break;

                default:
                    throw new NotImplementedException();
                // if (!Print(pv.Value, code))
                //     throw new InvalidOperationException();

            }
        }

        private void PrintString(ProtoPrintState s)
        {
            PrintHeaderBytes(s.CurrentField.HeaderBytes, s);

            var il = s.IL;


            s.LocalString ??= il.DeclareLocal(typeof(String));

            il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
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
        }

        private void ScanString(ProtoScanState s)
        {
            var il = s.IL;

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            //holdForSet = holdForSet ?? il.DeclareLocal(typeof(String));

            //Get length of string's bytes
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);
            il.Emit(OpCodes.Stloc, s.LastByteLocal);
                            
            //read bytes into buffer field
            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
            il.Emit(OpCodes.Ldsfld, _readBytes);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Callvirt, _readStreamBytes);
            il.Emit(OpCodes.Pop);

            //base.Utf8.GetString(fieldByteArray);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _utf8);
            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
            il.Emit(OpCodes.Ldsfld, _readBytes);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Call, _bytesToString);
            s.SetCurrentValue(il);
        }
    }
}
