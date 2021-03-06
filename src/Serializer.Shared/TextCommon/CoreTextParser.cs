﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace Das.Serializer
{
    public class CoreTextParser : CoreNumeric,
                                  ITextParser
    {
        static CoreTextParser()
        {
            _stringBuilder = new ThreadLocal<StringSaver>(GetNewSaver);
            _myStringBuilder = new ThreadLocal<StringSaver>(GetNewSaver);
            _stringBuilderQueue = new ConcurrentQueue<StringSaver>();
        }


        public CoreTextParser()
        {
            _enUs = new CultureInfo("en-US");
        }


        public String? FindJsonValue(String input,
                                     String toFind)
        {
            var hold = 0;
            return FindJsonValue(input, toFind, ref hold);
        }

        public IEnumerable<String> EnumerateJsonValues(String input,
                                                       String key)
        {
            var hold = 0;

            while (true)
            {
                var res = FindJsonValue(input, key, ref hold);
                if (res == null)
                    yield break;
                yield return res;
            }
        }


        public Boolean ContainsAll(String str,
                                   String[] list)
        {
            foreach (var s in list)
            {
                if (!str.Contains(s))
                    return false;
            }

            return true;
        }

        public Double FindJsonDouble(String input,
                                     String toFind)
        {
            var hold = 0;
            var val = FindJsonValue(input, toFind, ref hold);
            return null == val ? Double.NaN : GetDouble(val);
        }

        public String? FindTextWithin(String input,
                                      String leftBounds,
                                      String rightBounds)
        {
            var rightIndex = input.IndexOf(rightBounds, StringComparison.OrdinalIgnoreCase);
            var leftIndex = input.IndexOf(leftBounds, StringComparison.OrdinalIgnoreCase);
            if (rightIndex == -1 || leftIndex == -1)
                return null;

            leftIndex += leftBounds.Length;

            return input.Substring(leftIndex, rightIndex - leftIndex);
        }

        public String LeftOf(String input,
                             String rightBounds)
        {
            var index = input.IndexOf(rightBounds, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
                return input;
            return input.Substring(0, index);
        }

        public Boolean TryFindTextWithin(String input,
                                         String[] delimiters,
                                         out String[] found)
        {
            if (delimiters.Length == 1)
            {
                found = new[] {After(input, delimiters[0])};
                return true;
            }

            var res = new List<String>();
            foreach (var item in FindTextWithinImpl(input, delimiters))
            {
                // ReSharper disable once ReplaceWithStringIsNullOrEmpty
                if (String.IsNullOrWhiteSpace(item))
                    continue;
                res.Add(item!);
            }

            found = res.ToArray();

            if (res.Count >= 1)
                return true;

            found = default!;
            return false;
        }


        public IEnumerable<String> FindTextWithin(String input,
                                                  String[] delimiters)
        {
            foreach (var item in FindTextWithinImpl(input, delimiters))
            {
                if (item == null)
                    throw new InvalidOperationException();
                yield return item;
            }
        }

        public Boolean TryFindTextSurrounding(String input,
                                              String[] delimiters,
                                              out String[] found,
                                              Int32 findRequired = -1)
        {
            findRequired = findRequired == -1 ? delimiters.Length + 1 : findRequired;

            var start = 0;
            var res = new List<String>();
            var c = 0;

            for (; c < delimiters.Length; c++)
            {
                var end = input.IndexOf(delimiters[c], start, StringComparison.Ordinal);

                if (end != start)
                {
                    if (end == -1)
                        break;

                    var current = input.Substring(start, end - start);
                    res.Add(current);

                    if (res.Count == findRequired)
                        break;
                }

                start = end + delimiters[c].Length;
            }

            if (start > 0 && start < input.Length && res.Count < findRequired)
            {
                var current = input.Substring(start);
                if (current.Length > 0)
                    res.Add(current);
            }


            if (res.Count < findRequired)
            {
                found = default!;
                return false;
            }

            found = res.ToArray();
            return true;
        }

        public IEnumerable<String> FindTextsWithin(String input,
                                                   String leftOf,
                                                   String rightOf)
        {
            var endFirst = input.IndexOf(leftOf, StringComparison.OrdinalIgnoreCase);

            var first = input.Substring(0, endFirst);
            yield return first;

            var startSecond = input.IndexOf(rightOf, endFirst + first.Length,
                StringComparison.OrdinalIgnoreCase) + rightOf.Length;

            yield return input.Substring(startSecond);
        }

        public IEnumerable<String> FindTextWithin(String input,
                                                  String
                                                      leftBounds,
                                                  String middleBounds,
                                                  String rightBounds)
        {
            var leftIndex = input.IndexOf(leftBounds, StringComparison.OrdinalIgnoreCase);
            if (leftIndex == -1)
                yield break;

            var middleIndex = input.IndexOf(middleBounds, leftIndex, StringComparison.OrdinalIgnoreCase);
            if (middleIndex == -1)
                yield break;

            var rightIndex = middleIndex > 0
                ? input.IndexOf(rightBounds, middleIndex, StringComparison.OrdinalIgnoreCase)
                : input.IndexOf(rightBounds, leftIndex, StringComparison.OrdinalIgnoreCase);

            if (rightIndex == -1)
                yield break;

            leftIndex += leftBounds.Length;

            //if (middleIndex == -1)
            //    yield return input.Substring(leftIndex, rightIndex - leftIndex);
            //else
            {
                yield return input.Substring(leftIndex, middleIndex - leftIndex);

                middleIndex += middleBounds.Length;
                yield return input.Substring(middleIndex, rightIndex - middleIndex);
            }
        }

        public String? FindTextWithin(String input,
                                      String leftBounds,
                                      String rightBounds,
                                      Int32 startIndex)
        {
            var rightIndex = input.IndexOf(rightBounds, startIndex,
                StringComparison.OrdinalIgnoreCase);
            var leftIndex = input.IndexOf(leftBounds, StringComparison.OrdinalIgnoreCase);
            if (rightIndex == -1 || leftIndex == -1)
                return null;

            leftIndex += leftBounds.Length;

            return input.Substring(leftIndex, rightIndex - leftIndex);
        }

        String ITextParser.After(String inText,
                                 String afterFound)
        {
            return After(inText, afterFound);
        }

        public String RemoveAll(String input,
                                String[] toRemove)
        {
            var sb = new StringBuilder(input);
            foreach (var rip in toRemove)
            {
                sb.Replace(rip, String.Empty);
            }

            return sb.ToString();
        }

        public Boolean EndsWithOrdinal(String checkingString,
                                       String value)
        {
            return value != null && checkingString != null &&
                   checkingString.EndsWith(value,
                       StringComparison.OrdinalIgnoreCase);
        }

        public Boolean ContainsOrdinal(String checkingString,
                                       String value)
        {
            return value != null && checkingString != null &&
                   checkingString.IndexOf(value,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        ///     Returns the rest of the text if the text started with whenStartsWith
        /// </summary>
        public Boolean TrySkip(String inText,
                               String whenStartsWith,
                               out String remaining)
        {
            if (inText.StartsWith(whenStartsWith, StringComparison.Ordinal))
            {
                remaining = inText.Substring(whenStartsWith.Length);
                return true;
            }

            remaining = null!;
            return false;
        }

        public IEnumerator<String?> FindJsonValues(String input,
                                                   String toFind1,
                                                   String toFind2)
        {
            var index = 0;
            yield return NextOrNull(input, toFind1, ref index);
            yield return NextOrNull(input, toFind2, ref index);
        }

        public IEnumerator<String?> FindJsonValues(String input,
                                                   String toFind1,
                                                   String toFind2,
                                                   String toFind3)
        {
            var index = 0;

            yield return NextOrNull(input, toFind1, ref index);
            yield return NextOrNull(input, toFind2, ref index);
            yield return NextOrNull(input, toFind3, ref index);
        }

        public IEnumerator<String?> FindJsonValues(String input,
                                                   String toFind1,
                                                   String toFind2,
                                                   String toFind3,
                                                   String toFind4)
        {
            var index = 0;

            yield return NextOrNull(input, toFind1, ref index);
            yield return NextOrNull(input, toFind2, ref index);
            yield return NextOrNull(input, toFind3, ref index);
            yield return NextOrNull(input, toFind4, ref index);
        }

        public IEnumerator<String?> FindJsonValues(String input,
                                                   String toFind1,
                                                   String toFind2,
                                                   String toFind3,
                                                   String toFind4,
                                                   String toFind5)
        {
            var index = 0;

            yield return NextOrNull(input, toFind1, ref index);
            yield return NextOrNull(input, toFind2, ref index);
            yield return NextOrNull(input, toFind3, ref index);
            yield return NextOrNull(input, toFind4, ref index);
            yield return NextOrNull(input, toFind5, ref index);
        }

        public IEnumerator<String?> FindJsonValues(String input,
                                                   String toFind1,
                                                   String toFind2,
                                                   String toFind3,
                                                   String toFind4,
                                                   String toFind5,
                                                   String toFind6)
        {
            var index = 0;

            yield return NextOrNull(input, toFind1, ref index);
            yield return NextOrNull(input, toFind2, ref index);
            yield return NextOrNull(input, toFind3, ref index);
            yield return NextOrNull(input, toFind4, ref index);
            yield return NextOrNull(input, toFind5, ref index);
            yield return NextOrNull(input, toFind6, ref index);
        }

        public IEnumerator<String?> FindJsonValues(String input,
                                                   String toFind1,
                                                   String toFind2,
                                                   String toFind3,
                                                   String toFind4,
                                                   String toFind5,
                                                   String toFind6,
                                                   String toFind7)
        {
            var index = 0;

            yield return NextOrNull(input, toFind1, ref index);
            yield return NextOrNull(input, toFind2, ref index);
            yield return NextOrNull(input, toFind3, ref index);
            yield return NextOrNull(input, toFind4, ref index);
            yield return NextOrNull(input, toFind5, ref index);
            yield return NextOrNull(input, toFind6, ref index);
            yield return NextOrNull(input, toFind7, ref index);
        }

        public String? FindJsonValue(String json,
                                     String item,
                                     ref Int32 startIndex)
        {
            var pos = json.IndexOf("\"" + item + "\":", startIndex, StringComparison.Ordinal);
            if (pos == -1)
            {
                startIndex = -1;
                return null;
            }

            var beg = json.IndexOf(":", pos + 2, StringComparison.Ordinal);
            if (beg == -1) return String.Empty;

            var end = -1;

            for (var c = beg + 1; c < json.Length; c++)
            {
                var current = json[c];
                switch (current)
                {
                    case Const.Space:
                        break;
                    case Const.Quote:
                        startIndex = c;
                        var res = GetJsonStringValue(json, ref startIndex);
                        return res;
                    case '[':
                        //array

                        end = json.IndexOf(']', c) + 1;
                        beg = c - 1;
                        goto afterLoop;
                    default:
                        //has to be a number
                        end = json.IndexOf(",", beg + 1, StringComparison.Ordinal);
                        goto afterLoop;
                }
            }

            afterLoop:

            startIndex = end;
            if (end == -1)
            {
                if (beg > 0)
                {
                    end = json.IndexOf("}", beg, StringComparison.Ordinal);
                    if (end == -1)
                        return String.Empty;
                }
                else
                    return String.Empty;
            }

            if (beg >= end)
                return null;

            var value = json.Substring(beg + 1, end - (beg + 1));
            return value;
        }

        public String AsUSString(Double amount,
                                 Int32 decimalPlaces)
        {
            if (double.IsInfinity(amount))
            {
            }

            switch (decimalPlaces)
            {
                case 2:
                    return amount.ToString("0.00", _enUs);
                case 1:
                    return amount.ToString("0.0", _enUs);
                case 0:
                    return amount.ToString("0", _enUs);
                default:
                    return amount.ToString("0." + String.Empty.PadLeft(decimalPlaces), _enUs);
            }
        }

        public String AsUSString(Decimal amount,
                                 Int32 decimalPlaces)
        {
            switch (decimalPlaces)
            {
                case 2:
                    return amount.ToString("0.00", _enUs);
                case 1:
                    return amount.ToString("0.0", _enUs);
                case 0:
                    return amount.ToString("0", _enUs);
                default:
                    return amount.ToString("0." + String.Empty.PadLeft(decimalPlaces), _enUs);
            }
        }

        public String AsUSString(Decimal amount)
        {
            return amount.ToString("0.00", _enUs);
        }

        public String AsUSString(Double amount)
        {
            return amount.ToString("0.00", _enUs);
        }

        public Boolean AppendAsUsCommaString(StringBuilder sb,
                                             IEnumerable<Double> amountList,
                                             Int32 multiple,
                                             Int32 decimalPlaces)
        {
            using (var iter = amountList.GetEnumerator())
            {
                if (!iter.MoveNext())
                    return false;

                sb.Append(AsUSString(iter.Current * multiple, decimalPlaces));

                while (iter.MoveNext())
                    sb.Append(", " + AsUSString(iter.Current * multiple, decimalPlaces));

                return true;
            }
        }

        public Int32 IndexOfEnd(String searchIn,
                                String searchFor)
        {
            var start = searchIn.IndexOf(searchFor, StringComparison.Ordinal);
            return start == -1 ? -1 : start + searchFor.Length;
        }


        public Int32 IndexOf(String searchIn,
                             String searchFor)
        {
            return searchIn.IndexOf(searchFor, StringComparison.OrdinalIgnoreCase);
        }

        public Boolean TryIndexOf(String searchIn,
                                  String searchFor,
                                  out Int32 index)
        {
            index = searchIn.IndexOf(searchFor, StringComparison.OrdinalIgnoreCase);
            return index >= 0;
        }

        public Int32 LastIndexOf(String searchIn,
                                 String searchFor)
        {
            return searchIn.LastIndexOf(searchFor, StringComparison.OrdinalIgnoreCase);
        }

        public Int32 LastIndexOf(String searchIn,
                                 String searchFor,
                                 Int32 startIndex)
        {
            return searchIn.LastIndexOf(searchFor, startIndex,
                StringComparison.OrdinalIgnoreCase);
        }

        public Int32 IndexOf(String searchIn,
                             String searchFor,
                             Int32 startIndex)
        {
            return searchIn.IndexOf(searchFor, startIndex,
                StringComparison.OrdinalIgnoreCase);
        }


        public String[] GetLines(String str)
        {
            return str.Split(_splitTokens, StringSplitOptions.RemoveEmptyEntries);
        }

        public String BuildString<T1, T2, T3, T4>(T1 item1,
                                                  T2 item2,
                                                  T3 item3,
                                                  T4 item4)
        {
            using (var sb = _myStringBuilder.Value)
            {
                sb!.Clear();
                sb.Append(item1);
                sb.Append(item2);
                sb.Append(item3);
                sb.Append(item4);

                return sb.ToString();
            }
        }

        public String BuildString<T1, T2, T3, T4, T5>(T1 item1,
                                                      T2 item2,
                                                      T3 item3,
                                                      T4 item4,
                                                      T5 item5)
        {
            using (var sb = _myStringBuilder.Value)
            {
                sb!.Clear();
                sb.Append(item1);
                sb.Append(item2);
                sb.Append(item3);
                sb.Append(item4);
                sb.Append(item5);

                return sb.ToString();
            }
        }

        public String BuildString<T1, T2, T3, T4, T5, T6>(T1 item1,
                                                          T2 item2,
                                                          T3 item3,
                                                          T4 item4,
                                                          T5 item5,
                                                          T6 item6)
        {
            using (var sb = _myStringBuilder.Value)
            {
                sb!.Clear();
                sb.Append(item1);
                sb.Append(item2);
                sb.Append(item3);
                sb.Append(item4);
                sb.Append(item5);
                sb.Append(item6);

                return sb.ToString();
            }
        }

        public ITextRemunerable GetThreadsStringBuilder(String initial)
        {
            var sb = GetThreadsStringBuilder();

            sb.Append(initial);
            return sb;
        }

        public ITextRemunerable GetThreadsStringBuilder()
        {
            var sb = _stringBuilder.Value;
            if (!sb!.IsEmpty)
                throw new ObjectDisposedException("Stringbuilder was not disposed " + sb);
            sb.Undispose();

            return sb;
        }

        public ITextRemunerable BorrowStringBuilder(String initial)
        {
            if (!_stringBuilderQueue.TryDequeue(out var sb))
                sb = new StringSaver(initial, OnStringSaverDisposed);
            else
            {
                sb.Undispose();
                sb.Append(initial);
            }

            return sb;
        }

        public ITextRemunerable GetThreadsStringBuilder(Int32 capacity)
        {
            var sb = GetThreadsStringBuilder();
            sb.Capacity = capacity;
            return sb;
        }

        private static StringBuilder GetBuilder => _findBuilder
                                                   ?? (_findBuilder = new StringBuilder());

        public static String After(String inText,
                                   String afterFound)
        {
            var index = inText.IndexOf(afterFound, StringComparison.Ordinal);
            if (index == -1)
                return inText;

            return inText.Substring(index + afterFound.Length);
        }

        public IEnumerable<Tuple<String, Int32>> FindJsonValues(String json,
                                                                String item)
        {
            var index = 0;
            do
            {
                var found = FindJsonValue(json, item, ref index);
                if (found == null)
                    yield break;

                yield return new Tuple<String, Int32>(found, index);
            } while (index >= 0);
        }

        private static IEnumerable<String?> FindTextWithinImpl(String input,
                                                               String[] delimiters)
        {
            var current = 0;

            for (var c = 0; c + 1 < delimiters.Length; c++)
            {
                var start = input.IndexOf(delimiters[c], current, StringComparison.Ordinal);
                if (start == -1)
                {
                    yield return null;
                    yield break;
                }

                start += delimiters[c].Length;

                var end = input.IndexOf(delimiters[c + 1], start, StringComparison.Ordinal);
                if (end == -1)
                {
                    yield return null;
                    yield break;
                }

                current = end;

                yield return input.Substring(start, end - start);
            }
        }

        private static String GetJsonStringValue(String json,
                                                 ref Int32 beg)
        {
            var sb = GetBuilder;
            var isCurrentEscaped = false;
            var c = beg + 1;

            for (; c < json.Length; c++)
            {
                var current = json[c];

                switch (current)
                {
                    case Const.BackSlash:
                        if (isCurrentEscaped)
                            sb.Append(current);
                        isCurrentEscaped = !isCurrentEscaped;
                        break;
                    case Const.Quote:
                        if (!isCurrentEscaped)
                            goto afterLoop;
                        goto default;
                    default:
                        sb.Append(current);
                        isCurrentEscaped = false;
                        break;
                }
            }

            afterLoop:
            var wal = sb.ToString();
            sb.Clear();
            beg = c;
            return wal;
        }

        private static StringSaver GetNewSaver()
        {
            return new();
        }


        private String? NextOrNull(String input,
                                   String toFind,
                                   ref Int32 index)
        {
            if (index == -1 || !TryFindValueWithRetry(input, toFind, ref index, out var result))
                return null;
            return result;
        }

        private static void OnStringSaverDisposed(StringSaver sb)
        {
            _stringBuilderQueue.Enqueue(sb);
        }

        private Boolean TryFindValueWithRetry(String json,
                                              String item,
                                              ref Int32 startIndex,
                                              out String? result)
        {
            result = FindJsonValue(json, item, ref startIndex);
            if (startIndex != -1)
                return true;

            startIndex = 0;
            result = FindJsonValue(json, item, ref startIndex);
            if (startIndex == -1)
                return false;

            return true;
        }

        private static readonly ThreadLocal<StringSaver> _stringBuilder;
        private static readonly ThreadLocal<StringSaver> _myStringBuilder;
        private static readonly ConcurrentQueue<StringSaver> _stringBuilderQueue;

        [ThreadStatic]
        private static StringBuilder? _findBuilder;

        private readonly CultureInfo _enUs;
        private readonly String[] _splitTokens = {"\r\n", "\r", "\n"};
    }
}
