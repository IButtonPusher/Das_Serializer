using System;
using System.Text;
using System.Threading;

namespace Das.Serializer.Scanners
{
    public class JsonPrimitiveScanner : StringPrimitiveScanner
    {
        public JsonPrimitiveScanner(ISerializationContext state) : base(state)
        {
        }

        private static StringBuilder Builder => _buffer.Value;

        private static readonly ThreadLocal<StringBuilder> _buffer
            = new ThreadLocal<StringBuilder>(() => new StringBuilder());

        public override String Descape(String input)
        {
            var isEscaped = false;

            foreach (var ct in input)
            {
                if (ct != Const.BACKSLASH)
                    continue;

                isEscaped = true;
                break;
            }

            if (!isEscaped)
                return input;

            var sbString = Builder;

            sbString.Clear();

            // ReSharper disable once TooWideLocalVariableScope
            Char c;
            var i = 0;
            using (var enumerator = input.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    c = enumerator.Current;

                    if (c == 92)
                    {
                        enumerator.MoveNext();
                        c = enumerator.Current;
                        i++;

                        switch (c)
                        {
                            case 'b':
                                sbString.Append('\b');
                                break;
                            case 't':
                                sbString.Append('\t');
                                break;
                            case 'n':
                                sbString.Append('\n');
                                break;
                            case 'f':
                                sbString.Append('\f');
                                break;
                            case 'r':
                                sbString.Append(Const.CarriageReturn);
                                break;
                            case '"':
                                sbString.Append(Const.Quote);
                                break;
                            case 'u':
                                var unicode = input.Substring(i + 1, 4);
                                var unichar = Convert.ToInt32(unicode, 16);
                                sbString.Append((Char) unichar);

                                for (var j = 0; j < 4; j++)
                                    enumerator.MoveNext();
                                i += 4;
                                break;
                        }
                    }


                    else
                        sbString.Append(c);

                    i++;
                }
            }

            return sbString.ToString();
        }
    }
}