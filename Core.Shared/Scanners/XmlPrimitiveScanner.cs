using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class XmlPrimitiveScanner : StringPrimitiveScanner
    {
        static XmlPrimitiveScanner()
        {
            Entities = new Dictionary<String, String>
            {
                {"&amp;", "&"},
                {"&lt;", "<"},
                {"&gt;", ">"},
                {"&quot;", "\""},
                {"&apos;", "\'"}
            };
        }

        public XmlPrimitiveScanner(ISerializationContext state) : base(state)
        {
        }

        private static Dictionary<String, String> Entities { get; }

        public override String Descape(String input)
        {
            return HtmlDecode(input);
        }

        //https://github.com/mono/mono/blob/master/mcs/class/System.Web/System.Web.Util/HttpEncoder.cs
        public static String HtmlDecode(String? s)
        {
            if (s == null)
                return null!;

            if (s.Length == 0)
                return String.Empty;

            if (s.IndexOf('&') == -1)
                return s;
            var rawEntity = RawEntity.Value;
            var entity = Entity.Value;
            var output = Output.Value;

            rawEntity.Clear();
            entity.Clear();
            output.Clear();

            var len = s.Length;
            // 0 -> nothing,
            // 1 -> right after '&'
            // 2 -> between '&' and ';' but no '#'
            // 3 -> '#' found after '&' and getting numbers
            var state = 0;
            var number = 0;
            var isHexValue = false;
            var haveTrailingDigits = false;

            for (var i = 0; i < len; i++)
            {
                var c = s[i];
                if (state == 0)
                {
                    if (c == '&')
                    {
                        entity.Append(c);
                        rawEntity.Append(c);
                        state = 1;
                    }
                    else
                    {
                        output.Append(c);
                    }

                    continue;
                }

                if (c == '&')
                {
                    state = 1;
                    if (haveTrailingDigits)
                    {
                        entity.Append(number.ToString(CultureInfo.InvariantCulture));
                        haveTrailingDigits = false;
                    }

                    output.Append(entity);
                    entity.Length = 0;
                    entity.Append('&');
                    continue;
                }

                if (state == 1)
                {
                    if (c == ';')
                    {
                        state = 0;
                        output.Append(entity);
                        output.Append(c);
                        entity.Length = 0;
                    }
                    else
                    {
                        number = 0;
                        isHexValue = false;
                        if (c != '#')
                            state = 2;
                        else
                            state = 3;

                        entity.Append(c);
                        rawEntity.Append(c);
                    }
                }
                else if (state == 2)
                {
                    entity.Append(c);
                    if (c != ';')
                        continue;

                    var key = entity.ToString();
                    if (key.Length > 1 && Entities.TryGetValue(key, out key))
                        output.Append(key);
                    state = 0;
                    entity.Length = 0;
                    rawEntity.Length = 0;
                }
                else if (state == 3)
                {
                    if (c == ';')
                    {
                        if (number == 0)
                        {
                            output.Append(rawEntity + ";");
                        }
                        else if (number > 65535)
                        {
                            output.Append("&#");
                            output.Append(number.ToString(CultureInfo.InvariantCulture));
                            output.Append(";");
                        }
                        else
                        {
                            output.Append((Char) number);
                        }

                        state = 0;
                        entity.Length = 0;
                        rawEntity.Length = 0;
                        haveTrailingDigits = false;
                    }
                    else if (isHexValue && Uri.IsHexDigit(c))
                    {
                        number = number * 16 + Uri.FromHex(c);
                        haveTrailingDigits = true;
                        rawEntity.Append(c);
                    }
                    else if (Char.IsDigit(c))
                    {
                        number = number * 10 + (c - '0');
                        haveTrailingDigits = true;
                        rawEntity.Append(c);
                    }
                    else if (number == 0 && (c == 'x' || c == 'X'))
                    {
                        isHexValue = true;
                        rawEntity.Append(c);
                    }
                    else
                    {
                        state = 2;
                        if (haveTrailingDigits)
                        {
                            entity.Append(number.ToString(CultureInfo.InvariantCulture));
                            haveTrailingDigits = false;
                        }

                        entity.Append(c);
                    }
                }
            }

            if (entity.Length > 0)
                output.Append(entity);
            else if (haveTrailingDigits) output.Append(number.ToString(CultureInfo.InvariantCulture));

            return output.ToString();
        }

        private static readonly ThreadLocal<StringBuilder> RawEntity
            = new ThreadLocal<StringBuilder>(() => new StringBuilder());

        private static readonly ThreadLocal<StringBuilder> Entity
            = new ThreadLocal<StringBuilder>(() => new StringBuilder());

        private static readonly ThreadLocal<StringBuilder> Output
            = new ThreadLocal<StringBuilder>(() => new StringBuilder());
    }
}