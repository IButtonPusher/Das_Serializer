using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public readonly struct AttributeValue
{
   public AttributeValue(String value,
                         Boolean wasValueInQuotes)
   {
      Value = value;
      WasValueInQuotes = wasValueInQuotes;
   }

   public override string ToString()
   {
      if (WasValueInQuotes)
         return "\"" + Value + "\"";

      return Value;
   }

   public readonly String Value;
   public readonly Boolean WasValueInQuotes;
}