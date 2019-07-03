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
        public  void Append(string data)
		{
			_sb.Append(data);
		}

        [MethodImpl(256)]
        public  void Append(string data1, string data2)
        {
            _sb.Append(data1);
            _sb.Append(data2);
        }

       

        [MethodImpl(256)]
        public  void Append(char data1, string data2)
        {
            _sb.Append(data1);
            _sb.Append(data2);
        }

        public void Append(IEnumerable<string> datas)
        {
            foreach (var data in datas)
                _sb.Append(data);
        }

        public override string ToString() => _sb.ToString();


        [MethodImpl(256)]
        public void Dispose()
		{
			_sb.Clear();
		}


        [MethodImpl(256)]
        public void Append(char data)
		{
			_sb.Append(data);
		}

        public bool IsEmpty => _sb.Length == 0;
       
    }

	
}
