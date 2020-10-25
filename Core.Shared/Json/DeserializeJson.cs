using System;
using System.IO;
using System.Threading.Tasks;
using Das.Extensions;
using Das.Streamers;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public Object FromJson(String json)
        {
            using (var state = StateProvider.BorrowJson(Settings))
            {
                return state.Scanner.Deserialize<Object>(json);
            }
        }

        public T FromJson<T>(String json)
        {
            //return JsonExpress.Deserialize<T>(json);

            using (var state = StateProvider.BorrowJson(Settings))
            {
                var res = state.Scanner.Deserialize<T>(json);
                return res;
            }
        }

        public T FromJsonEx<T>(String json)
        {
            return JsonExpress.Deserialize<T>(json);
        }

        public T FromJson<T>(FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                return FromJson<T>(fs);
            }
        }

        public virtual T FromJson<T>(Stream stream)
        {
            var streamWrap = new StreamStreamer(stream);
            return FromJsonCharArray<T>(streamWrap);
        }

        public virtual async Task<T> FromJsonAsync<T>(Stream stream)
        {
            stream.Position = 0;
            var buffer = new Byte[(Int32) stream.Length];
            await _readAsync(stream, buffer, 0, buffer.Length);
            var encoding = buffer.GetEncoding();
            var chars = encoding.GetChars(buffer, 0, buffer.Length);

            using (var state = StateProvider.BorrowJson(Settings))
            {
                var res = state.Scanner.Deserialize<T>(chars);
                return res;
            }


            // return await Task.Factory.StartNew(() => FromJson<T>(stream));
        }

        public override void Dispose()
        {
        }

        protected virtual T FromJsonCharArray<T>(Char[] json)
        {
            using (var state = StateProvider.BorrowJson(Settings))
            {
                var res = state.Scanner.Deserialize<T>(json);
                return res;
            }
        }

        //private JsonExpress GetNewJsonExpress()
        //{
        //    return new JsonExpress(ObjectInstantiator, TypeManipulator, TypeInferrer);
        //}
    }
}