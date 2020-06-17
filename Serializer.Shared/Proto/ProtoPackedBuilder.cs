using Das.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Das.Serializer.Proto;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private void ScanAsPackedArray(ILGenerator il, Type fieldType)
        {
            if (!(GetPackedArrayType(fieldType) is {} packType))
                throw new NotSupportedException();

            ////////////////////////////////////////////////////////
            // EXTRACT COLLECTION FROM STREAM
            ////////////////////////////////////////////////////////

            //Get the number of bytes we will be using
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getPositiveInt32);

            if (packType == typeof(Int32))
                il.Emit(OpCodes.Call, _extractPackedInt32Itar);

            else if (packType == typeof(Int16))
                il.Emit(OpCodes.Call, _extractPackedInt16Itar);

            else if (packType == typeof(Int64))
                il.Emit(OpCodes.Call, _extractPackedInt64Itar);

            //convert the value on the stack to an array for direct assignment
            var linqToArray = typeof(Enumerable).GetMethodOrDie(
                nameof(Enumerable.ToArray), Const.PublicStatic);
            linqToArray = linqToArray.MakeGenericMethod(packType);

            il.Emit(OpCodes.Call, linqToArray);
        }

        private void PrintAsPackedArray(ProtoPrintState s)
        {
            var type = s.CurrentField.Type;
            var il = s.IL;

            PrintHeaderBytes(s.CurrentField.HeaderBytes, s);
            
            s.LoadParentToStack();

            if (!(GetPackedArrayType(type) is {} packType)) 
                throw new InvalidOperationException("Cannot print " + type + " as a packed repeated field");

            /////////////////////////////////////
            // var arrayLocalField = obj.Property;
            /////////////////////////////////////
            var arrayLocalField = il.DeclareLocal(type);

            il.Emit(OpCodes.Call, s.CurrentField.GetMethod);
            il.Emit(OpCodes.Stloc, arrayLocalField);
            /////////////////////////////////////

            /////////////////////////////////////
            // WriteInt32(GetPackedArrayLength(ienum)); 
            /////////////////////////////////////
            MethodInfo getPackedArrayLength = null!;

            if (packType == typeof(Int32))
                getPackedArrayLength = _getPackedInt32Length.MakeGenericMethod(type);
            else if (packType == typeof(Int16))
                getPackedArrayLength = _getPackedInt16Length.MakeGenericMethod(type);
            else if (packType == typeof(Int64))
                getPackedArrayLength = _getPackedInt64Length.MakeGenericMethod(type);

            //il.Emit(OpCodes.Ldarg_0);


            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, arrayLocalField);
            il.Emit(OpCodes.Call, getPackedArrayLength);
                
            s.WriteInt32();
          
            MethodInfo writePackedArray = null!;
                
            if (packType == typeof(Int32))
                writePackedArray = _writePacked32.MakeGenericMethod(type);
            else if (packType == typeof(Int16))
                writePackedArray = _writePacked16.MakeGenericMethod(type);
            else if (packType == typeof(Int64))
                writePackedArray = _writePacked64.MakeGenericMethod(type);

                
            il.Emit(OpCodes.Ldloc, arrayLocalField);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, writePackedArray);
        }

        private static Type? GetPackedArrayType(Type propertyType)
        {
            if (typeof(IEnumerable<Int32>).IsAssignableFrom(propertyType))
                return typeof(Int32);

            if (typeof(IEnumerable<Int16>).IsAssignableFrom(propertyType))
                return typeof(Int16);

            if (typeof(IEnumerable<Int64>).IsAssignableFrom(propertyType))
                return typeof(Int64);

            return default;
        }
    }
}
