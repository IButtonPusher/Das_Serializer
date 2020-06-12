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
                // if (!Print(pv.Value, code))
                //     throw new InvalidOperationException();

            }
        }

        private void PrintPrimitive(ProtoPrintState s)
        {
            var pv = s.CurrentField;

            var code = pv.TypeCode;
            var il = s.IL;

            PrintHeaderBytes(pv.HeaderBytes, s);
            
            //s.LoadProxyToStack();
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
                // if (!Print(pv.Value, code))
                //     throw new InvalidOperationException();

            }
        }

        private void PrintString(ProtoPrintState s, Action<ProtoPrintState> pushStringValueToStack)
        {
            PrintHeaderBytes(s.CurrentField.HeaderBytes, s);

            var il = s.IL;

            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldsfld, _utf8); //this._utf8...

            pushStringValueToStack(s);
            //s.LoadCurrentFieldValueToStack(); //P_0.StringField

            

           // s.LocalString ??= il.DeclareLocal(typeof(String));

            //il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
            //il.Emit(OpCodes.Stloc, s.LocalString);

            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldfld, s.UtfField);
           // il.Emit(OpCodes.Ldloc, s.LocalString);

            il.Emit(OpCodes.Callvirt, _getStringBytes);
            il.Emit(OpCodes.Stloc, s.LocalBytes);

            il.Emit(OpCodes.Ldloc, s.LocalBytes);
            il.Emit(OpCodes.Call, _getArrayLength);
            s.WriteInt32(); // WriteInt32(bytes.Length, P_1);


            //il.Emit(OpCodes.Call, _writeInt32);

            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, s.LocalBytes);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, _writeBytes);


            //s.LoadCurrentFieldValueToStack();

            //var il = s.IL;

            //s.LocalString ??= il.DeclareLocal(typeof(String));

            //il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
            //il.Emit(OpCodes.Stloc, s.LocalString);

            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldfld, s.UtfField);
            //il.Emit(OpCodes.Ldloc, s.LocalString);

            //il.Emit(OpCodes.Callvirt, _getStringBytes);
            //il.Emit(OpCodes.Stloc, s.LocalBytes);

            //il.Emit(OpCodes.Ldloc, s.LocalBytes);
            //il.Emit(OpCodes.Call, _getArrayLength);
            //s.WriteInt32();
            ////il.Emit(OpCodes.Call, _writeInt32);

            ////il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldloc, s.LocalBytes);
            //il.Emit(OpCodes.Call, _writeBytes);
        }

        private void ScanString(ProtoScanState s, 
            Action<ILGenerator>? setCurrentValue,
            ref LocalBuilder holdForSet)
        {
            var il = s.IL;

            //il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            //holdForSet = holdForSet ?? il.DeclareLocal(typeof(String));

            //Get length of string's bytes
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);
            il.Emit(OpCodes.Stloc, s.LastByteLocal);
                            
            //read bytes into buffer field
            //il.Emit(OpCodes.Ldloc, s.ByteBufferField);
            il.Emit(OpCodes.Ldsfld, _readBytesField);


            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Callvirt, _readStreamBytes);
            il.Emit(OpCodes.Pop);
            //il.Emit(OpCodes.Pop);

            //return;

            if (setCurrentValue != null)
                s.LoadCurrentValueOntoStack(il);
            else
                holdForSet = holdForSet ??  il.DeclareLocal(typeof(String));

            //s.LoadCurrentValueOntoStack(il);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _utf8);
            
            il.Emit(OpCodes.Ldsfld, _readBytesField);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Call, _bytesToString);
            
            
            if (setCurrentValue != null)
                setCurrentValue(il);
            else
                il.Emit(OpCodes.Stloc, holdForSet);
            
            //setCurrentValue(il);
        }

        private void ScanStringIntoField(ProtoScanState s, Action<ILGenerator> setCurrentValue)
        {
            var il = s.IL;

            il.Emit(OpCodes.Ldarg_1);

            ///////////////////////////////////////////
            // var lastByteLocal = base.GetPositiveInt32(P_0))
            //////////////////////////////////////////
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);
            il.Emit(OpCodes.Stloc, s.LastByteLocal);
                            
            //read bytes into buffer field
            
            ///////////////////////////////////////////
            // P_0.Read(_readBytesField, 0, lastByteLocal)
            //////////////////////////////////////////
            il.Emit(OpCodes.Ldsfld, _readBytesField);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Callvirt, _readStreamBytes);
            il.Emit(OpCodes.Pop);

            ///////////////////////////////////////////
            // CurrentField = Utf8.GetString(_readBytesField, 0, lastByteLocal)
            //////////////////////////////////////////
            s.LoadCurrentValueOntoStack(il);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _utf8);
            
            il.Emit(OpCodes.Ldsfld, _readBytesField);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Call, _bytesToString);
            setCurrentValue(il);
        }

        private void ScanStringOntoStack(ProtoScanState s)
        {
            var il = s.IL;

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _utf8);


            il.Emit(OpCodes.Ldc_I4_0);

            ///////////////////////////////////////////
            // var lastByteLocal = base.GetPositiveInt32(P_0))
            //////////////////////////////////////////
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32); //2nd parameter
            //il.Emit(OpCodes.Stloc, s.LastByteLocal);
                            
            //read bytes into buffer field
            
            ///////////////////////////////////////////
            // P_0.Read(_readBytesField, 0, lastByteLocal)
            //////////////////////////////////////////
            il.Emit(OpCodes.Ldsfld, _readBytesField);

            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Callvirt, _readStreamBytes);
            il.Emit(OpCodes.Pop);

            //s.LoadCurrentValueOntoStack(il);

            ///////////////////////////////////////////
            // CurrentField = this.Utf8.GetString(_readBytesField, 0, lastByteLocal)
            //////////////////////////////////////////
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, _utf8);
            
            il.Emit(OpCodes.Ldsfld, _readBytesField);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ldloc, s.LastByteLocal);
            il.Emit(OpCodes.Call, _bytesToString);
            //s.SetCurrentValue(il);
        }
    }
}
