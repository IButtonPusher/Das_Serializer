#if GENERATECODE

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.Proto;
using Das.Serializer.State;

namespace Das.Serializer.ProtoBuf
{
    public partial class ProtoPrintState
    {
        public void PrintByteArrayField()
        {
            PrintHeaderBytes(CurrentFieldHeader);

            LoadParentToStack();

            _il.Emit(OpCodes.Call, CurrentField.GetMethod);
            _il.Emit(OpCodes.Stloc, LocalBytes);

            _il.Emit(OpCodes.Ldloc, LocalBytes);
            _il.Emit(OpCodes.Call, _getArrayLength);
            WriteInt32();

            _il.Emit(OpCodes.Ldloc, LocalBytes);

            _il.Emit(OpCodes.Ldarg_2);
            _il.Emit(OpCodes.Call, _writeBytes);
            
        }

        public void PrintObjectArray()
        {
            var pv = CurrentField;
            var ienum = new DynamicEnumerator<IProtoPrintState>(this, pv.Type,
                pv.GetMethod, _types, _actionProvider);

            ienum.ForLoop(PrintEnumeratorCurrent);
        }

        public void PrintPrimitiveArray()
        {
            PrintCollection(PrintEnumeratorCurrent);
        }

        public void PrintIntCollection()
        {
            var type = CurrentField.Type;
            var il = IL;

            PrintHeaderBytes(CurrentFieldHeader);

            LoadParentToStack();

            var packTypeCode = GetPackedArrayTypeCode(type);

            /////////////////////////////////////
            // var arrayLocalField = obj.Property;
            /////////////////////////////////////
            var arrayLocalField = il.DeclareLocal(type);

            il.Emit(OpCodes.Call, CurrentField.GetMethod);
            il.Emit(OpCodes.Stloc, arrayLocalField);
            /////////////////////////////////////

            /////////////////////////////////////
            // WriteInt32(GetPackedArrayLength(ienum)); 
            /////////////////////////////////////
            MethodInfo getPackedArrayLength;
            MethodInfo writePackedArray;

            switch (packTypeCode)
            {
                case TypeCode.Int32:
                    getPackedArrayLength = _getPackedInt32Length.MakeGenericMethod(type);
                    writePackedArray = _writePacked32.MakeGenericMethod(type);
                    break;

                case TypeCode.Int16:
                    getPackedArrayLength = _getPackedInt16Length.MakeGenericMethod(type);
                    writePackedArray = _writePacked16.MakeGenericMethod(type);
                    break;

                case TypeCode.Int64:
                    getPackedArrayLength = _getPackedInt64Length.MakeGenericMethod(type);
                    writePackedArray = _writePacked64.MakeGenericMethod(type);
                    break;

                default:
                    throw new InvalidOperationException("Cannot print " + type + " as a packed repeated field");
            }


            il.Emit(OpCodes.Ldloc, arrayLocalField);
            il.Emit(OpCodes.Call, getPackedArrayLength);

            WriteInt32();


            il.Emit(OpCodes.Ldloc, arrayLocalField);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, writePackedArray);
        }

       

        

        public void PrintObjectCollection()
        {
            PrintCollection(PrintEnumeratorCurrent);
        }

        public void PrintPrimitiveCollection()
        {
            PrintCollection(PrintEnumeratorCurrent);
        }

        public void PrintDictionary()
        {
            PrintCollection(PrintKeyValuePair);
        }

        public void AppendPrimitive<TData>(TData data,
                                           TypeCode typeCode,
                                           Action<IDynamicPrintState, TData> loadValue)
        {
            loadValue(this, data);

            switch (typeCode)
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

        public void AppendChar(Char c)
        {
            throw new NotSupportedException();
        }

        public void AppendDateTime()
        {
            throw new NotImplementedException();
        }

        public void AppendNull()
        {
            throw new NotSupportedException();
        }

        public void PrintAsPackedArray()
        {
            PrintIntCollection();
        }

        private void PrintCollection(OnValueReady action)
        {
            var pv = CurrentField;
            var ienum = new DynamicEnumerator<IProtoPrintState>(this, 
                pv.Type, pv.GetMethod, _types, _actionProvider);

            ienum.ForEach(action);
        }

        //private void PrintArray()
        //{
        //    var pv = CurrentField;
        //    var ienum = new ProtoEnumerator<IProtoPrintState>(this, pv.Type, pv.GetMethod, _types);

        //    ienum.ForLoop(PrintEnumeratorCurrent);
        //}

        private void PrintKeyValuePair(LocalBuilder enumeratorCurrentValue,
                                       Type itemType,
                                       FieldAction fieldAction)
        {
            PrintFieldViaProxy(ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
        }

        private void PrintEnumeratorCurrent(LocalBuilder enumeratorCurrentValue,
                                            LocalBuilder currentIndex,
                                            Type itemType,
                                            FieldAction fieldAction)
        {
            PrintEnumeratorCurrent(enumeratorCurrentValue, itemType, fieldAction);
        }

        private void PrintEnumeratorCurrent(LocalBuilder enumeratorCurrentValue,
                                            Type itemType,
                                            FieldAction fieldAction)
        {
            switch (fieldAction)
            {
                case FieldAction.ChildObject:
                    PrintChildObjectField(ilg => ilg.Emit(OpCodes.Ldloc, enumeratorCurrentValue),
                        itemType);
                    break;

                case FieldAction.String:
                    PrintStringWithHeader(xs => xs.IL.Emit(OpCodes.Ldloc, enumeratorCurrentValue));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

      
    }
}

#endif