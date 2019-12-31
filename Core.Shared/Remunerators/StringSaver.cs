using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
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

        public override String ToString() => _sb.ToString();


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

        public void CopyTo(Int32 sourceIndex, Char[] destination, Int32 destinationIndex, Int32 count)
        {
            _sb.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}