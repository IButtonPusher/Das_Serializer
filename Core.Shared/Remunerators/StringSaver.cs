using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Das.Extensions;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public class StringSaver : StringBase, ITextRemunerable, ITextAccessor
    {
        private StringBuilder _sb;

        private static readonly Object _sbLock;
        private static readonly List<StringBuilder> _sbPool;

        static StringSaver()
        {
            _sbLock = new Object();
            _sbPool = new List<StringBuilder>();
        }

        public StringSaver()
        {
            SetBackingBuilder();
        }

        private void SetBackingBuilder()
        {
            lock (_sbLock)
            {
                if (_sbPool.Count > 0)
                {
                    _sb = _sbPool[0];
                    _sbPool.RemoveAt(0);
                    return;
                }
            }
            _sb = new StringBuilder();
        }

        // ReSharper disable once UnusedMember.Global
        public StringSaver(String seed)
        {
            _sb = new StringBuilder(seed);
        }

        void IRemunerable<String>.Append(String str, Int32 cnt)
        {
            throw new NotSupportedException();
        }


        [MethodImpl(256)]
        public void Append(String data)
        {
            var len = _sb.Length + data.Length;
            EnsureCapacity(len);

            _sb.Append(data);
        }

        [MethodImpl(256)]
        public void Append(Object obj)
        {
            _sb.Append(obj);
        }

        [MethodImpl(256)]
        public void Append(String data1, String data2)
        {
            var len = _sb.Length + data1.Length + data2.Length;
            EnsureCapacity(len);

            _sb.Append(data1);
            _sb.Append(data2);
        }

        private void TryGetBiggerBackingBuilder(Int32 capacity)
        {
            var newI = -1;

            lock (_sbLock)
            {
                var half = _sbPool.Count / 2.0;
                if (half.IsZero())
                    return;
                var i = Convert.ToInt32(half);
                var current = _sbPool[i];
                if (current.Capacity > capacity)
                {
                    //we need smaller than average
                    for (; i >= 0; i--)
                    {
                        current = _sbPool[i];
                        if (current.Capacity >= capacity) 
                            continue;

                        //ok that's too small now
                        newI = i + 1;
                        goto isHaveIt;
                    }
                }
                else
                {
                    //we need greater than average
                    for (; i < _sbPool.Count; i++)
                    {
                        current = _sbPool[i];
                        if (current.Capacity <= capacity) 
                            continue;

                        //ok that's too big now
                        newI = i - 1;
                        goto isHaveIt;
                    }
                }

                isHaveIt:
                if (newI == -1)
                    return;

                var letsUse = _sbPool[newI];
                _sbPool.RemoveAt(newI);

                letsUse.Append(_sb);
                
                Recycle(_sb);
                
                _sb = letsUse;
            }
        }

        [MethodImpl(256)]
        public void Append(String data1, String data2, String data3)
        {
            var len = _sb.Length + data1.Length + data2.Length + data3.Length;
            EnsureCapacity(len);

            _sb.Append(data1);
            _sb.Append(data2);
            _sb.Append(data3);
        }


        [MethodImpl(256)]
        public void Append(Char data1, String data2)
        {
            var len = _sb.Length + 1 + data2.Length;
            EnsureCapacity(len);

            _sb.Append(data1);
            _sb.Append(data2);
        }

        private void EnsureCapacity(Int32 len)
        {
            if (len > _sb.Capacity)
                TryGetBiggerBackingBuilder(len);
        }

        public void Append(ITextAccessor txt)
        {
            var len = _sb.Length + 1 + txt.Length;
            EnsureCapacity(len);

            _sb.Append(txt);
        }

        public Boolean Append<T>(IList<T> items, Char separator)
        {
            if (items.Count == 0)
                return false;

            _sb.Append(items[0]);

            for (var c = 1; c < items.Count; c++)
            {
                _sb.Append(separator);
                _sb.Append(items[c]);
            }

            return true;
        }

        public Boolean Append<T>(IEnumerable<T> items, Char separator)
        where T : IConvertible
        {
            using (var itar = items.GetEnumerator())
            {
                if (!itar.MoveNext())
                    return false;

                _sb.Append(itar.Current);

                while (itar.MoveNext())
                {
                    _sb.Append(separator);
                    _sb.Append(itar.Current);
                }
            }

            return true;
        }

        public void Append(IEnumerable<String> datas)
        {
            foreach (var data in datas)
                _sb.Append(data);
        }

        public void Append<T>(T data) where T : struct
        {
            _sb.Append(data.ToString());
        }

        public void Append(String[] datas, Char separator)
        {
            if (datas.Length == 0)
                return;

            var len = 0;
            EnsureCapacity(len);

            for (var c = 0; c < datas.Length; c++)
            {
                len += datas[c].Length + 1;
            }

            for (var c = 0; c < datas.Length - 1; c++)
            {
                _sb.Append(datas[c]);
                _sb.Append(separator);
            }

            _sb.Append(datas[datas.Length-1]);
        }

        public override String ToString() => _sb.ToString();

        public ITextAccessor ToImmutable()
        {
            return new StringAccessor(_sb.ToString());
        }

        public static implicit operator StringBuilder(StringSaver sv) => sv._sb;

        [MethodImpl(256)]
        public void Dispose()
        {
            Recycle(_sb);
            _sb = null!;
        }

        public void Undispose()
        {
            if (_sb != null)
                return;
            SetBackingBuilder();
        }

        private static void Recycle(StringBuilder sb)
        {
            var capacity = sb.Capacity;

            sb.Clear();

            lock (_sbLock)
            {
                var half = _sbPool.Count / 2.0;
                if (half.IsZero())
                {
                    _sbPool.Add(sb);
                    return;
                }

                var i = Convert.ToInt32(half);
                var current = _sbPool[i];
                if (current.Capacity > capacity)
                {
                    for (; i >= 0; i--)
                    {
                        current = _sbPool[i];
                        if (current.Capacity < capacity)
                        {
                            _sbPool.Insert(i + 1, sb);
                            return;
                        }
                    }

                    _sbPool.Insert(0, sb);
                }
                else
                {
                    for (; i < _sbPool.Count; i++)
                    {
                        current = _sbPool[i];
                        if (current.Capacity > capacity)
                        {
                            _sbPool.Insert(i - 1, sb);
                            return;
                        }
                    }

                    _sbPool.Add(sb);
                }
            }
        }

        [MethodImpl(256)]
        public void Append(Char data)
        {
            _sb.Append(data);
        }

        // ReSharper disable once UnusedMember.Global
        public void Remove(Int32 startIndex, Int32 length) => _sb.Remove(startIndex, length);

        public Boolean IsEmpty => _sb == null || _sb.Length == 0;

        public Char this[Int32 index] => _sb[index];

        public Boolean Contains(String str, StringComparison comparison)
            => _sb.ToString().IndexOf(str, comparison) >= 0;

        public Int32 Length => _sb.Length;

        public Boolean IsNullOrWhiteSpace()
        {
            if (_sb.Length == 0)
                return true;

            for (var c = 0; c < _sb.Length; c++)
            {
                if (!Char.IsWhiteSpace(_sb[c]))
                    return false;
            }

            return true;
        }

        public String[] Split()
        {
            return _sb.ToString().Split();
        }

        public String[] Split(Char[] separators, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
        {
            return _sb.ToString().Split(separators, options);
        }

        public String[] TrimAndSplit()
        {
            return _sb.ToString().Trim().Split();
        }

        public String Remove(ISet<Char> chars)
        {
            return Remove(chars, _sb.ToString());
        }

        public String Substring(Int32 start, Int32 length)
        {
            return _sb.ToString(start, length);
        }

        public String Substring(Int32 start)
        {
            return _sb.ToString(start, _sb.Length - start);
        }

        public Boolean Contains(Char c)
        {
            for (var i = 0; i < _sb.Length; i++)
                if (_sb[i] == c)
                    return true;

            return false;
        }

        public Boolean Contains(String str)
        {
            return _sb.ToString().IndexOf(str, StringComparison.Ordinal) >= 0;
        }

        public Int32 Count(Char c)
        {
            var cnt = 0;
            for (var i = 0; i < _sb.Length; i++)
                if (_sb[i] == c)
                    cnt++;

            return cnt;
        }

        public void TrimEnd()
        {
            for (var c = _sb.Length - 1; c >= 0; c--)
            {
                if (_sb[c] == ' ')
                    _sb.Remove(c, 1);
                else
                    break;
                
            }
        }

        public void Clear()
        {
            if (_sb == null)
                Undispose();
            _sb.Clear();
        }

        public void CopyTo(Int32 sourceIndex, Char[] destination, Int32 destinationIndex, Int32 count)
        {
            _sb.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}