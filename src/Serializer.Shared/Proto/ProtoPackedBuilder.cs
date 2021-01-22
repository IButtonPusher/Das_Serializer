#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Extensions;


namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private Boolean CanScanAndAddPackedArray(Type fieldType, out TypeCode typeCode)
        {
            typeCode = TypeCode.Empty;

            if (!fieldType.IsGenericType || !_types.IsCollection(fieldType))
                return false;

            var germane = _types.GetGermaneType(fieldType);
            var iColl = typeof(ICollection<>).MakeGenericType(germane);
            if (!iColl.IsAssignableFrom(fieldType))
                return false;

            typeCode = Type.GetTypeCode(germane);

            switch (typeCode)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
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

        //private MethodInfo? GetScanAndAddPackedMethod(Type fieldType)
        //{
        //    if (!fieldType.IsGenericType || !_types.IsCollection(fieldType))
        //        return null;

        //    var germane = _types.GetGermaneType(fieldType);
        //    var iColl = typeof(ICollection<>).MakeGenericType(germane);
        //    if (!iColl.IsAssignableFrom(fieldType))
        //        return null;

        //    MethodInfo? baseAdd;


        //    switch (Type.GetTypeCode(germane))
        //    {
        //        case TypeCode.Int16:
        //            baseAdd = typeof(ProtoDynamicBase).GetMethodOrDie(
        //                nameof(ProtoDynamicBase.AddPacked16));
        //            break;

        //        case TypeCode.Int32:
        //            baseAdd = typeof(ProtoDynamicBase).GetMethodOrDie(
        //                nameof(ProtoDynamicBase.AddPacked32));
        //            break;

        //        case TypeCode.Int64:
        //            baseAdd = typeof(ProtoDynamicBase).GetMethodOrDie(
        //                nameof(ProtoDynamicBase.AddPacked64));
        //            break;

        //        default:
        //            return null;
        //    }

        //    return baseAdd.MakeGenericMethod(fieldType);
        //}

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

        private void ScanAsPackedArray(ILGenerator il,
                                       Type fieldType,
                                       Boolean isCanAddToCollection)
        {
            if (!(GetPackedArrayType(fieldType) is {} packType))
                throw new NotSupportedException();

            if (isCanAddToCollection && CanScanAndAddPackedArray(fieldType, out _))
                return;

            ////////////////////////////////////////////////////////
            // EXTRACT COLLECTION FROM STREAM
            ////////////////////////////////////////////////////////

            //Get the number of bytes we will be using
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, GetPositiveInt32);


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
    }
}

#endif