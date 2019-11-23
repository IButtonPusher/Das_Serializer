﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Streamers;

namespace Das
{
    public partial class DasCoreSerializer
    {
        public Object FromXml(String xml) => _FromXml<Object>(xml);

        public T FromXml<T>(String xml) => _FromXml<T>(xml);

        public async Task<T> FromXml<T>(FileInfo file)
        {
            using (TextReader tr = new StreamReader(file.FullName))
            {
                var txt = await _readToEndAsync(tr);
                var arr = txt.ToCharArray();
                return _FromXml<T>(arr);
            }
        }

        public T FromXml<T>(IEnumerable<Char> xml)
        {
            using (var state = StateProvider.BorrowXml(Settings))
                return state.Scanner.Deserialize<T>(xml);
        }

        public T FromXml<T>(Stream stream)
        {
            using (var streamWrap = new StreamStreamer(stream))
            using (var state = StateProvider.BorrowXml(Settings))
                return state.Scanner.Deserialize<T>(streamWrap);
        }

        [MethodImpl(256)]
        private T _FromXml<T>(String xml) => _FromXml<T>(xml.ToCharArray());

        [MethodImpl(256)]
        private T _FromXml<T>(Char[] xml)
        {
            using (var state = StateProvider.BorrowXml(Settings))
                return state.ArrayScanner.Deserialize<T>(xml);
        }
        public Object FromXml(Stream stream) => FromXml<Object>(stream);
    }
}