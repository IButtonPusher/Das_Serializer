using System;
using System.Globalization;


namespace Das.Serializer
{
    public abstract class StringPrimitiveScanner : IStringPrimitiveScanner
    {
        public StringPrimitiveScanner(ISerializationContext state)
        {
            _state = state;
        }

        private readonly ISerializationContext _state;

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

            if (type == Const.ObjectType)
                return input;

            var conv = _state.GetTypeConverter(type);
            return conv.ConvertFromInvariantString(input);
        }

        public abstract String Descape(String input);
    }
}