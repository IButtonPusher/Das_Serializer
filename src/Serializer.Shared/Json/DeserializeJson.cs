using System;
using System.IO;
using System.Threading.Tasks;
using Das.Extensions;

#if !ALWAYS_EXPRESS

#endif

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public Object FromJson(String json)
        {
            return JsonExpress.Deserialize<Object>(json, Settings, _empty);
        }

        public T FromJson<T>(String json)
        {
            return JsonExpress.Deserialize<T>(json, Settings, _empty);
        }

        public object FromJson(String json,
                               Type type)
        {
            return JsonExpress.Deserialize(json, type, Settings, _empty);
        }

        //public T FromJsonEx<T>(String json)
        //{
        //    return JsonExpress.Deserialize<T>(json, Settings, _empty);
        //}

        //public T FromJsonEx<T>(String json,
        //                       Object[] ctorValues)
        //{
        //    return JsonExpress.Deserialize<T>(json, Settings, ctorValues);
        //}

        public T FromJson<T>(FileInfo file)
        {
            //#if ALWAYS_EXPRESS

            String txt = GetTextFromFileInfo(file);

            return JsonExpress.Deserialize<T>(txt, Settings, _empty);
            //#else
            //using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            //{
            //    return FromJson<T>(fs);
            //}
            //#endif
        }

        public virtual T FromJson<T>(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var txt = sr.ReadToEnd();
                return JsonExpress.Deserialize<T>(txt, Settings, _empty);
            }
        }

        public virtual async Task<T> FromJsonAsync<T>(Stream stream)
        {
            stream.Position = 0;
            var buffer = new Byte[(Int32) stream.Length];
            await _readAsync(stream, buffer, 0, buffer.Length);
            var encoding = buffer.GetEncoding();


            var str = encoding.GetString(buffer, 0, buffer.Length);
            return JsonExpress.Deserialize<T>(str, Settings, _empty);
        }

        public override void Dispose()
        {
        }


        private static readonly Object[] _empty = new Object[0];
    }
}
