using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Das.Remunerators
{
	internal class BufferedStringSaver : StringSaverBase, IRemunerable<String, Char>
    {
        readonly StreamWriter _writer;

		public BufferedStringSaver(Stream stream)
		{
			_writer = new StreamWriter(stream);
		}


        [MethodImpl(256)]
        public override void Append(string data)
        {
            _writer.Write(data);
        }

        [MethodImpl(256)]
        public override void Append(string data1, string data2)
        {
            _writer.Write(data1);
            _writer.Write(data2);
        }

        [MethodImpl(256)]
        public override void Append(char data1, string data2)
        {
            _writer.Write(data1);
            _writer.Write(data2);
        }

        [MethodImpl(256)]
        public override void Append(char data1, char data2)
        {
            _writer.Write(data1);
            _writer.Write(data2);
        }


//        [MethodImpl(256)]
//        public  void Append(string data1, string data2, string data3, string data4)
//        {
//            Append(data1, data2, data3);
//            _writer.Write(data4);
//        }

//        [MethodImpl(256)]
//        public void Append(string data1, string data2, string data3, string data4, string data5)
//        {
//            Append(data1, data2, data3, data4);
//            _writer.Write(data5);
//        }

        [MethodImpl(256)]
        public override void Append(char data)
		{
			_writer.Write(data);
		}

      

        public  bool IsEmpty => _writer.BaseStream.Length == 0;

       

        public  void Dispose()
		{
			_writer.Dispose();
		}

        public  void AppendFormat(string str, int c)
        {
            _writer.Write(str, c);
        }
    }
}
