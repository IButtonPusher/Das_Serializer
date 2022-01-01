using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Das.Extensions;

#if !ALWAYS_EXPRESS

#endif

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public Object FromXml(String xml)
        {
            //#if ALWAYS_EXPRESS
            return XmlExpress.Deserialize<Object>(xml, Settings, _empty);
            //#else
            //return _FromXml<Object>(xml);
            //#endif
        }

        public T FromXml<T>(String xml)
        {
            //#if ALWAYS_EXPRESS
            return XmlExpress.Deserialize<T>(xml, Settings, _empty);
            //#else
            //return _FromXml<T>(xml);
            //#endif
        }

        public async Task<T> FromXmlAsync<T>(Stream stream)
        {
            stream.Position = 0;
            var buffer = new Byte[(Int32) stream.Length];
            await _readAsync(stream, buffer, 0, buffer.Length);
            var encoding = buffer.GetEncoding();
            var txt = encoding.GetString(buffer, 0, buffer.Length);
            return XmlExpress.Deserialize<T>(txt, Settings, _empty);

            //using (TextReader tr = new StreamReader(stream))
            //{
            //    var xml = await _readToEndAsync(tr).ConfigureAwait(true);
            //    return XmlExpress.Deserialize<T>(xml, Settings, _empty);
            //}
        }

        //public T FromXmlEx<T>(String xml)
        //{
        //    return XmlExpress.Deserialize<T>(xml, Settings, _empty);
        //}

        public IEnumerable<T> FromXmlItems<T>(String xml)
        {
            return XmlExpress.DeserializeMany<T>(xml);
        }

        public async Task<T> FromXmlAsync<T>(FileInfo file)
        {
            var txt = await GetTextFromFileInfoAsync(file);

            return XmlExpress.Deserialize<T>(txt, Settings, _empty);
        }

        public T FromXml<T>(Stream stream)
        {
            using (var sw = new StreamReader(stream))
            {
                var str = sw.ReadToEnd();
                return XmlExpress.Deserialize<T>(str, Settings, _empty);
            }

            //using (var streamWrap = new StreamStreamer(stream))
            //using (var state = StateProvider.BorrowXml(Settings))
            //{
            //    return state.Scanner.Deserialize<T>(streamWrap);
            //}
        }

        public Object FromXml(Stream stream)
        {
            return FromXml<Object>(stream);
        }

        //#if !ALWAYS_EXPRESS
        #if FALSE
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
