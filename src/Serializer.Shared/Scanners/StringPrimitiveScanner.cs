﻿using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Das.Serializer;

public abstract class StringPrimitiveScanner : IStringPrimitiveScanner
{
   protected StringPrimitiveScanner(ITypeCore state)
   {
      _state = state;
   }

   public Object GetValue(String? input,
                          Type type,
                          Boolean wasInputInQuotes)
   {
      if (ReferenceEquals(null, input))
         return default!;

      if (string.Equals(string.Empty, input))
      {
         if (type == Const.StrType)
            return input;

         return default!;
      }

      if (string.IsNullOrEmpty(input))
         return default!;

      if (type == Const.ObjectType)
      {
         //could be a number or boolean if it's not in quotes
         if (Boolean.TryParse(input, out var b))
            return b;

         if (CanValueBeNumber(wasInputInQuotes))
         {
            if (Int64.TryParse(input, out var big))
            {
               if (big > Int32.MaxValue)
                  return big;
               return (Int32) big;
            }

            if (Decimal.TryParse(input, out var dec))
               return dec;
         }
      }
      else if (type.IsEnum)
      {
         if (ReferenceEquals(null, input))
         {
            var enumVals = Enum.GetValues(type);
            return enumVals.GetValue(0);
         }

         return Enum.Parse(type, input);
      }

      else if (type == Const.StrType)
         return Descape(input);

      else if (Const.IConvertible.IsAssignableFrom(type))
         return Convert.ChangeType(input, type, CultureInfo.InvariantCulture);

      else
      {
         var ctor = type.GetConstructor(new[] {typeof(String)});
         if (ctor != null) return Activator.CreateInstance(type, input);
      }

      if (type == Const.ObjectType)
         return input;

      //todo: this is probably not good
      if (input == "null")
         return null!;

      return _state.ConvertFromInvariantString(input, type);
      //var conv = _state.GetTypeConverter(type);
      //return conv.ConvertFromInvariantString(input)!;
   }

   public abstract String Descape(String? input);

   protected abstract Boolean CanValueBeNumber(Boolean wasInputInQuotes);

   private readonly ITypeCore _state;
}