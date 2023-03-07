#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Reflection.Common;

namespace Das.Serializer.ProtoBuf
{
    // ReSharper disable once UnusedType.Global
    // ReSharper disable once UnusedTypeParameter
    public partial class ProtoDynamicProvider<TPropertyAttribute>
    {
        private Boolean CanScanAndAddPackedArray(Type fieldType,
                                                 out TypeCode typeCode)
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

        private static TypeCode GetPackedArrayTypeCode(Type propertyType)
        {
            if (typeof(IEnumerable<Int32>).IsAssignableFrom(propertyType))
                return TypeCode.Int32;

            if (typeof(IEnumerable<Int16>).IsAssignableFrom(propertyType))
                return TypeCode.Int16;

            return typeof(IEnumerable<Int64>).IsAssignableFrom(propertyType)
                ? TypeCode.Int64
                : TypeCode.Empty;
        }


       

        private void ScanAsPackedArray(ILGenerator il,
                                       Type fieldType,
                                       Boolean isCanAddToCollection)
        {
            var packTypeCode = GetPackedArrayTypeCode(fieldType);


            if (isCanAddToCollection && CanScanAndAddPackedArray(fieldType, out _))
                return;

            ////////////////////////////////////////////////////////
            // EXTRACT COLLECTION FROM STREAM
            ////////////////////////////////////////////////////////

            //Get the number of bytes we will be using
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, GetPositiveInt32);

            Type packType;

            switch (packTypeCode)
            {
                case TypeCode.Int32:
                    il.Emit(OpCodes.Call, _extractPackedInt32Itar);
                    packType = typeof(Int32);
                    break;

                case TypeCode.Int16:
                    il.Emit(OpCodes.Call, _extractPackedInt16Itar);
                    packType = typeof(Int16);
                    break;

                case TypeCode.Int64:
                    il.Emit(OpCodes.Call, _extractPackedInt64Itar);
                    packType = typeof(Int64);
                    break;

                default:
                    throw new InvalidOperationException("Cannot scan " + fieldType + " as a packed repeated field");
            }


            //convert the value on the stack to an array for direct assignment
            var linqToArray = typeof(Enumerable).GetMethodOrDie(
                nameof(Enumerable.ToArray), Const.PublicStatic);
            linqToArray = linqToArray.MakeGenericMethod(packType);

            il.Emit(OpCodes.Call, linqToArray);
        }
    }
}

#endif
