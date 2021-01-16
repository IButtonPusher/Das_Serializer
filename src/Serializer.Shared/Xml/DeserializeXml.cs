using System;
using System.Collections.Generic;
using System.IO;
#if !ALWAYS_EXPRESS

using System.Runtime.CompilerServices;
#endif
using System.Threading.Tasks;
using Das.Streamers;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public Object FromXml(String xml)
        {
            #if ALWAYS_EXPRESS
            return XmlExpress.Deserialize<Object>(xml);
            #else
            return _FromXml<Object>(xml);
            #endif
        }

        public T FromXml<T>(String xml)
        {
            #if ALWAYS_EXPRESS
            return XmlExpress.Deserialize<T>(xml);
            #else
            return _FromXml<T>(xml);
            #endif
        }

        public T FromXmlEx<T>(String xml)
        {
            return XmlExpress.Deserialize<T>(xml);
        }

        public IEnumerable<T> FromXmlItems<T>(String xml)
        {
            return XmlExpress.DeserializeMany<T>(xml);
        }

        public async Task<T> FromXmlAsync<T>(FileInfo file)
        {
            var txt = await GetTextFromFileInfoAsync(file);

            #if ALWAYS_EXPRESS
            return XmlExpress.Deserialize<T>(txt);
            #else
                var arr = txt.ToCharArray();
                return _FromXml<T>(arr);
            #endif

        }

        public T FromXml<T>(Char[] xml)
        {
            using (var state = StateProvider.BorrowXml(Settings))
            {
                return state.Scanner.Deserialize<T>(xml);
            }
        }

        public T FromXml<T>(Stream stream)
        {
            using (var streamWrap = new StreamStreamer(stream))
            using (var state = StateProvider.BorrowXml(Settings))
            {
                return state.Scanner.Deserialize<T>(streamWrap);
            }
        }

        public Object FromXml(Stream stream)
        {
            return FromXml<Object>(stream);
        }

        #if !ALWAYS_EXPRESS

        [MethodImpl(256)]
        private T _FromXml<T>(String xml)
        {
            using (var state = StateProvider.BorrowXml(Settings))
            {
                return state.Scanner.Deserialize<T>(xml);
            }
            //return _FromXml<T>(xml.ToCharArray());
        }

        [MethodImpl(256)]
        private T _FromXml<T>(Char[] xml)
        {
            using (var state = StateProvider.BorrowXml(Settings))
            {
                return state.ArrayScanner.Deserialize<T>(xml);
            }
        }

        #endif
    }
}