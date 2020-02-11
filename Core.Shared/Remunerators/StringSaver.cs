using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Core.Shared.TextCommon;
using Das.Serializer.Scanners;

namespace Das.Serializer.Remunerators
{
    public class StringSaver : ITextRemunerable, ITextAccessor
    {
        private readonly StringBuilder _sb;

        public StringSaver()
        {
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
            _sb.Append(data);
        }

        [MethodImpl(256)]
        public void Append(String data1, String data2)
        {
            _sb.Append(data1);
            _sb.Append(data2);
        }

        [MethodImpl(256)]
        public void Append(String data1, String data2, String data3)
        {
            _sb.Append(data1);
            _sb.Append(data2);
            _sb.Append(data3);
        }


        [MethodImpl(256)]
        public void Append(Char data1, String data2)
        {
            _sb.Append(data1);
            _sb.Append(data2);
        }

        public void Append(IEnumerable<String> datas)
        {
            foreach (var data in datas)
                _sb.Append(data);
        }

        public void Append(String[] datas, Char separator)
        {
            if (datas.Length == 0)
                return;

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


        [MethodImpl(256)]
        public void Dispose()
        {
            _sb.Clear();
        }

        [MethodImpl(256)]
        public void Append(Char data)
        {
            _sb.Append(data);
        }

        // ReSharper disable once UnusedMember.Global
        public void Remove(Int32 startIndex, Int32 length) => _sb.Remove(startIndex, length);

        public Boolean IsEmpty => _sb.Length == 0;

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
            //var cnt = Count(' ');
            //var res = new StringBuilder[cnt];
            return _sb.ToString().Split();
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
            _sb.Clear();
        }

        public void CopyTo(Int32 sourceIndex, Char[] destination, Int32 destinationIndex, Int32 count)
        {
            _sb.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}