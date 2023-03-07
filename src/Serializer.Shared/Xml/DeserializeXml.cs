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
            return XmlExpress.Deserialize<Object>(xml, Settings, _empty);
        }

        public T FromXml<T>(String xml)
        {
            return XmlExpress.Deserialize<T>(xml, Settings, _empty);
        }

        public async Task<T> FromXmlAsync<T>(Stream stream)
        {
            stream.Position = 0;
            var buffer = new Byte[(Int32) stream.Length];
            await _readAsync(stream, buffer, 0, buffer.Length);
            var encoding = buffer.GetEncoding();
            var txt = encoding.GetString(buffer, 0, buffer.Length);
            return XmlExpress.Deserialize<T>(txt, Settings, _empty);
        }

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
        }

        public Object FromXml(Stream stream)
        {
            return FromXml<Object>(stream);
        }
    }
}
