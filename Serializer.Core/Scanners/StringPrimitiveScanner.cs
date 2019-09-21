using System;
using System.ComponentModel;
using System.Globalization;
using Das.Serializer;
using Serializer;


namespace Das.Scanners
{
    public abstract class StringPrimitiveScanner : IStringPrimitiveScanner
    {
        public Object GetValue(String input, Type type)
        {
            if (type == Const.ObjectType)
            {
                //could be a number or boolean if it's not in quotes
                if (Boolean.TryParse(input, out var b))
                    return b;
                if (Int64.TryParse(input, out var big))
                {
                    if (big > Int32.MaxValue)
                        return big;
                    return (Int32) big;
                }

                if (Decimal.TryParse(input, out var dec))
                    return dec;
            }
            else if (type.IsEnum)
                return Enum.Parse(type, input);

            else if (type == Const.StrType)
                return Descape(input);

            else if (Const.IConvertible.IsAssignableFrom(type))
                return Convert.ChangeType(input, type, CultureInfo.InvariantCulture);

            else
            {
                var ctor = type.GetConstructor(new[] {typeof(String)});
                if (ctor != null)
                {
                    return Activator.CreateInstance(type, input);
                }
            }

            if (type == typeof(Object))
                return input;

            var conv = TypeDescriptor.GetConverter(type);
            return conv.ConvertFromInvariantString(input);
        }

        public abstract String Descape(String input);
    }
}