using Das.Streamers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Serializer.Core.Files;

namespace Das
{
	public partial class DasCoreSerializer
	{
		public Object FromXml(String xml)
		{
            using (var state = StateProvider.BorrowXml(Settings))
            {
                var res = state.Scanner.Deserialize<Object>(xml);
                return res;
            }
		}

		public T FromXml<T>(String xml) => _FromXml<T>(xml);

        public T FromXml<T>(FileInfo file)
		{
            using (var _ = new SafeFile(file))
            {
                using (var fs = file.OpenRead())
                    return FromXml<T>(fs);
            }
		}

		public T FromXml<T>(Stream stream)
		{
            using (var streamWrap = new StreamStreamer(stream))
                return _FromXml<T>(streamWrap);
		}

        [MethodImpl(256)]
        private T _FromXml<T>(IEnumerable<Char> xml)
        {
            using (var state = StateProvider.BorrowXml(Settings))
                return state.Scanner.Deserialize<T>(xml);
        }


		public Object FromXml(Stream stream) => FromXml<Object>(stream);
    }
}
