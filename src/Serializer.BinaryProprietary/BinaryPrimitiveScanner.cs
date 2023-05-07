using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Das.Extensions;
using Reflection.Common;
using Serializer.BinaryProprietary;

//using System.IO;

namespace Das.Serializer;

public class BinaryPrimitiveScanner : SerializerCore, IBinaryPrimitiveScanner
{
   public BinaryPrimitiveScanner(ISerializationCore dynamicFacade,
                                 ISerializerSettings settings)
      : base(dynamicFacade, settings)
   {
      //_fallbackFormatter = new BinaryFormatter();
      _instantiator = ObjectInstantiator;

      _typeSurrogates = new Dictionary<Type, Func<byte[], object?>>();

      var ts = TimeSpan.FromMilliseconds(4);
      

      _typeSurrogates.Add(typeof(TimeSpan),
         ba => TimeSpan.FromMilliseconds(_instantiator.CreatePrimitiveObject<Int32>(ba, typeof(Int32))));
   }

   private readonly Dictionary<Type, Func<Byte[], Object?>> _typeSurrogates;


   public T GetValue<T>(Byte[] input)
   {
      var type = typeof(T);

      if (TryGetValueImpl(input, type, out var res) && res is T good)
         return good;


      var scanFunc = BinarySurrogate<T>.ScanFunc;

      if (scanFunc != null )
      {
         return scanFunc(input, _instantiator);
      }

      if (_typeSurrogates.TryGetValue(type, out var surrogate) &&
          surrogate(input) is T good2)
         return good2;

      throw new InvalidOperationException($"Unable to deserialize from binary type {type}");
   }

   public Object? GetValue(Byte[] input,
                           Type type,
                           Boolean wasInputInQuotes)
   {
      if (TryGetValueImpl(input, type, out var res))
         return res;

      
      if (_typeSurrogates.TryGetValue(type, out var surrogate))
         return surrogate(input);

      var ggetValue = GetType()
                      .GetMethodOrDie(nameof(GetValue), typeof(Byte[]))
                      .MakeGenericMethod(type);

      return ggetValue.Invoke(this, new object[] { input });
      

      throw new InvalidOperationException($"Unable to deserialize from binary type {type}");

      //if (input == null)
      //   return null;

      //switch (Type.GetTypeCode(type))
      //{
      //   case TypeCode.String:
      //      return GetString(input);

      //   case TypeCode.Decimal:
      //      return ExtensionMethods.ToDecimal(input);

      //   case TypeCode.DateTime:
      //      var ticks = _instantiator.CreatePrimitiveObject<Int64>(input, typeof(Int64));
      //      return new DateTime(ticks);

      //   case TypeCode.Double:
      //      return BitConverter.ToDouble(input, 0);

      //   case TypeCode.Single:
      //      return BitConverter.ToSingle(input, 0);

      //   default:
      //      if (type.IsEnum)
      //      {
      //         res = _instantiator.CreatePrimitiveObject(input,
      //            Enum.GetUnderlyingType(type));
      //         res = Enum.ToObject(type, res);
      //      }
      //      else if (IsLeaf(type, false))
      //         res = _instantiator.CreatePrimitiveObject(input, type);
      //      else if (TryGetNullableType(type, out var primType))
      //         res = GetValue(input, primType, wasInputInQuotes);
      //      else if (_typeSurrogates.TryGetValue(type, out var surrogate))
      //         return surrogate(input);
      //      else
      //         throw new InvalidOperationException($"Unable to deserialize from binary type {type}");

      //      break;
      //}

      //return res;
   }

   private Boolean TryGetValueImpl(Byte[] input,
                                   Type type,
                                   out Object? value)
   {

      if (input == null)
      {
         value = default;
         return true;
      }

      switch (Type.GetTypeCode(type))
      {
         case TypeCode.String:
            value = GetString(input);
            return true;

         case TypeCode.Decimal:
            value = ExtensionMethods.ToDecimal(input);
            return true;

         case TypeCode.DateTime:
            var ticks = _instantiator.CreatePrimitiveObject<Int64>(input, typeof(Int64));
            value = new DateTime(ticks);
            return true;

         case TypeCode.Double:
            value = BitConverter.ToDouble(input, 0);
            return true;

         case TypeCode.Single:
            value = BitConverter.ToSingle(input, 0);
            return true;

         default:
            if (type.IsEnum)
            {
               value = _instantiator.CreatePrimitiveObject(input,
                  Enum.GetUnderlyingType(type));
               value = Enum.ToObject(type, value);
            }
            else if (IsLeaf(type, false))
               value = _instantiator.CreatePrimitiveObject(input, type);
            else if (TryGetNullableType(type, out var primType))
               value = GetValue(input, primType, false);
            //else if (_typeSurrogates.TryGetValue(type, out var surrogate))
            //   value = surrogate(input);
            else
            {
               value = default;
               return false;
               //throw new InvalidOperationException($"Unable to deserialize from binary type {type}");
            }

            break;
      }

      return true;
   }

   #if !PARTIALTRUST

   public unsafe Int32 GetInt32(Byte[] value)
   {
      fixed (Byte* pbyte = &value[0])
      {
         return *(Int32*)pbyte;
      }
   }

   String? IBinaryPrimitiveScanner.GetString(Byte[] tempByte) => GetString(tempByte);

   

   public static unsafe String? GetString(Byte[] tempByte)
   {
      if (tempByte == null)
         return null;

      fixed (Byte* bptr = tempByte)
      {
         var cptr = (Char*)bptr;
         return new String(cptr, 0, tempByte.Length / 2);
      }
   }

   #else
        public Int32 GetInt32(Byte[] value)
        {
            return BitConverter.ToInt32(value, 0);
        }


        public String? GetString(Byte[] tempByte)
        {


            if (tempByte == null)
                return null;

            return BitConverter.ToString(tempByte, 0);
        }


   #endif


   //private readonly BinaryFormatter _fallbackFormatter;
   private readonly IInstantiator _instantiator;
}
