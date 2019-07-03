using System;
using System.Text;
using System.Threading;
using Das.Scanners;

namespace Serializer.Core.Scanners
{
    public class JsonPrimitiveScanner : StringPrimitiveScanner
    {
        private static StringBuilder Builder => _buffer.Value;

        private static readonly ThreadLocal<StringBuilder> _buffer
            = new ThreadLocal<StringBuilder>(() => new StringBuilder());

        public override String Descape(string input)
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

            var _sbString = Builder;

            _sbString.Clear();

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
                                _sbString.Append('\b');
                                break;
                            case 't':
                                _sbString.Append('\t');
                                break;
                            case 'n':
                                _sbString.Append('\n');
                                break;
                            case 'f':
                                _sbString.Append('\f');
                                break;
                            case 'r':
                                _sbString.Append(Const.CarriageReturn);
                                break;
                            case '"':
                                _sbString.Append(Const.Quote);
                                break;
                            case 'u':
                                var unicode = input.Substring(i + 1, 4);
                                var unichar = Convert.ToInt32(unicode, 16);
                                _sbString.Append((Char)unichar);

                                for (var j = 0; j < 4; j++)
                                    enumerator.MoveNext();
                                i += 4;
                                break;
                        }
                    }


                    else
                        _sbString.Append(c);

                    i++;
                }
            }

            return _sbString.ToString();
        }
    }
}
