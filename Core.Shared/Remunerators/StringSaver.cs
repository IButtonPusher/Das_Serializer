using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Das.Remunerators
{
    internal class StringSaver : ITextRemunerable
    {
        private readonly StringBuilder _sb;

        public StringSaver()
        {
            _sb = new StringBuilder();
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

        public Boolean IsEmpty => _sb.Length == 0;
    }
}