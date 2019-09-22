using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Streamers;

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

        public async Task<T> FromXml<T>(FileInfo file)
        {
            using (TextReader tr = new StreamReader(file.FullName))
            {
                var txt = await _readToEndAsync(tr);
                return _FromXml<T>(txt);
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