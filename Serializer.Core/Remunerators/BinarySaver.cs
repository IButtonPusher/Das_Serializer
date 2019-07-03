using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Das.CoreExtensions;
using Serializer.Core.Binary;
using Serializer.Core.Printers;
using Serializer.Core.Remunerators;


namespace Das.Remunerators
{
	internal class BinarySaver : IRemunerable<Byte[], Byte>
	{
		private IBinaryWriter _writer;
        private BinaryLogger _logger;

        public BinarySaver(Stream stream)
        {
            _writer = new BinaryWriterWrapper(stream);
        }

        /// <summary>
        /// A reference type's byte payload is preceded by a length declaration.
        /// As we don't know the length till after we compute the payload, we use stacks
        /// </summary>
        public void Push(PrintNode node)
        {
            LogDebug("---->pushing " + node.Type.Name);
            _logger.TabPlus();
            _writer = _writer.Push(node);
        }

        public void Pop()
        {
            _logger.TabMinus();

            var popping = _writer;

            _writer = _writer.Pop();

            LogDebug("length of stacked data " + popping.Length + " " +
                BitConverter.GetBytes(popping.Length));
        }

        [Conditional("DEBUG")]
        public void LogDebug(String val)
        {
            _logger = _logger ??(_logger = new BinaryLogger());
            _logger.Debug(val);
        }

        [MethodImpl(256)]
        public void Append(byte[] data)
		{
            LogDebug("___APPEND___ " + data.Length + " " + data.ToString(','));
			_writer.Write(data);
		}

        public void Append(byte[] data, int limit)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(256)]
        public void Append(byte data)
        {
            LogDebug("___APPEND___ " + " " + data);
            
            _writer.Write(data);
        }

        public bool IsEmpty => _writer.IsEmpty;

        [MethodImpl(256)]
        public void WriteInt32(Int32 val)
		{
            LogDebug($"Writing INT32 {val} {BitConverter.GetBytes(val).ToString(',')}");
            _writer.WriteInt32(val);
		}

        public unsafe void WriteInt64(Int64 val)
        {
            LogDebug($"Writing INT64 {val} {BitConverter.GetBytes(val).ToString(',')}");
            var pi = (byte*)&val;
            _writer.Write(pi, 8);
        }

        public unsafe void WriteInt32(UInt32 val)
        {
            LogDebug($"Writing INT {val} {BitConverter.GetBytes(val).ToString(',')}");

            var pi = (byte*)&val;

            _writer.Write(pi[0]);
            _writer.Write(pi[1]);
            _writer.Write(pi[2]);
            _writer.Write(pi[3]);
        }

		public void Dispose()
		{
            var root = _writer.Pop();

            root.Flush();
            root.Dispose();
		}
	}
}
