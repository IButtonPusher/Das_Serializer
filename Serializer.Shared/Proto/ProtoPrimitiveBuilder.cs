using System;
using System.Reflection.Emit;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {

        private void PrintVarInt(ProtoPrintState s)
        {
            var pv = s.CurrentField;

            var code = pv.TypeCode;
            var il = s.IL;

            PrintHeaderBytes(pv.HeaderBytes, s);
            
            //s.LoadProxyToStack();
            s.LoadCurrentFieldValueToStack();
            il.Emit(OpCodes.Ldarg_2);

            switch (code)
            {
                case TypeCode.Int32:
                    il.Emit(OpCodes.Call, _writeInt32);
                    break;

                case TypeCode.Int64:
                    il.Emit(OpCodes.Call, _writeInt64);
                    break;

                case TypeCode.Int16:
                    il.Emit(OpCodes.Call, _writeInt16);
                    break;

                case TypeCode.Byte:
                    il.Emit(OpCodes.Call, _writeInt8);
                    break;

                case TypeCode.Boolean:
                    il.Emit(OpCodes.Call, _writeInt32);
                    break;

                default:
                    throw new NotImplementedException();

            }
        }

        private void PrintPrimitive(ProtoPrintState s)
        {
            var pv = s.CurrentField;

            var code = pv.TypeCode;
            var il = s.IL;

            PrintHeaderBytes(pv.HeaderBytes, s);

            s.LoadCurrentFieldValueToStack();

            switch (code)
            {
                case TypeCode.Single:
                    il.Emit(OpCodes.Call, _getSingleBytes);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, _writeBytes);
                    break;

                case TypeCode.Double:
                    il.Emit(OpCodes.Call, _getDoubleBytes);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, _writeBytes);
                    break;

                case TypeCode.Decimal:
                    il.Emit(OpCodes.Call, _getDoubleBytes);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, _writeBytes);
                    break;

                default:
                    throw new NotImplementedException();

            }
        }

        private void PrintString(ProtoPrintState s, Action<ProtoPrintState> pushStringValueToStack)
        {
            PrintHeaderBytes(s.CurrentField.HeaderBytes, s);

            var il = s.IL;

            il.Emit(OpCodes.Ldsfld, _utf8); //this._utf8...

            pushStringValueToStack(s);

            il.Emit(OpCodes.Callvirt, _getStringBytes);
            il.Emit(OpCodes.Stloc, s.LocalBytes);

            il.Emit(OpCodes.Ldloc, s.LocalBytes);
            il.Emit(OpCodes.Call, _getArrayLength);
            s.WriteInt32();

            il.Emit(OpCodes.Ldloc, s.LocalBytes);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, _writeBytes);
        }
    }
}
