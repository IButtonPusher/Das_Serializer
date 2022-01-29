#if GENERATECODE

using Das.Serializer.Proto;
using System;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.State;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoPrintState
    {
        public void PrintVarIntField()
        {
            PrintHeaderBytes(_currentField.HeaderBytes);

            LoadCurrentFieldValueToStack();
            _il.Emit(OpCodes.Ldarg_2);

            switch (_currentField.TypeCode)
            {
                case TypeCode.Int32:
                    _il.Emit(OpCodes.Call, _writeInt32);
                    break;

                case TypeCode.Int64:
                    _il.Emit(OpCodes.Call, _writeInt64);
                    break;

                case TypeCode.UInt64:
                    _il.Emit(OpCodes.Call, _writeUInt64);
                    break;

                case TypeCode.Int16:
                    _il.Emit(OpCodes.Call, _writeInt16);
                    break;

                case TypeCode.Byte:
                    _il.Emit(OpCodes.Call, _writeInt8);
                    break;

                case TypeCode.Boolean:
                    _il.Emit(OpCodes.Call, _writeInt32);
                    break;

                case TypeCode.UInt32:
                    _il.Emit(OpCodes.Call, _writeUInt32);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void PrintFallback()
        {
            throw new NotImplementedException();
        }

        public void PrintEnum()
        {
            LoadCurrentFieldValueToStack();
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _writeInt32);
        }

        public void PrintPrimitive()
        {
            PrintHeaderBytes(_currentField.HeaderBytes);
            LoadCurrentFieldValueToStack();

            switch (_currentField.TypeCode)
            {
                case TypeCode.Single:
                    _il.Emit(OpCodes.Call, _getSingleBytes);
                    _il.Emit(OpCodes.Ldarg_2);
                    _il.Emit(OpCodes.Call, _writeBytes);
                    break;

                case TypeCode.Double:
                    _il.Emit(OpCodes.Call, _getDoubleBytes);
                    _il.Emit(OpCodes.Ldarg_2);
                    _il.Emit(OpCodes.Call, _writeBytes);
                    break;

                case TypeCode.Decimal:
                    _il.Emit(OpCodes.Call, _getDoubleBytes);
                    _il.Emit(OpCodes.Ldarg_2);
                    _il.Emit(OpCodes.Call, _writeBytes);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void PrepareToPrintValue<TData>(TData data,
                                               Action<IDynamicState, TData> loadValue)
        {
            loadValue(this, data);
            _il.Emit(OpCodes.Ldarg_2);
        }

        public void PrepareToPrintValue()
        {
            LoadCurrentFieldValueToStack();
            _il.Emit(OpCodes.Ldarg_2);
        }

        public void PrintStringField()
        {
            PrintHeaderBytes(CurrentFieldHeader);
            PrintStringImpl(s => s.LoadCurrentFieldValueToStack());
            
        }

        private void PrintStringWithHeader(Action<IProtoPrintState> pushStringValueToStack)
        {
            PrintHeaderBytes(CurrentFieldHeader);
            PrintStringImpl(pushStringValueToStack);
        }

        private void PrintStringImpl(Action<IProtoPrintState> pushStringValueToStack)
        {
            //PrintHeaderBytes(CurrentFieldHeader);

            _il.Emit(OpCodes.Ldsfld, Utf8); //this._utf8...

            pushStringValueToStack(this);

            _il.Emit(OpCodes.Callvirt, _getStringBytes);
            _il.Emit(OpCodes.Stloc, LocalBytes);

            _il.Emit(OpCodes.Ldloc, LocalBytes);
            _il.Emit(OpCodes.Call, _getArrayLength);
            WriteInt32();

            _il.Emit(OpCodes.Ldloc, LocalBytes);
            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _writeBytes);
        }
    }
}


#endif