using Das.Streamers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public Object FromJson(String json)
        {
            using (var state = StateProvider.BorrowJson(Settings))
                return state.Scanner.Deserialize<Object>(json);
        }

        public T FromJson<T>(String json)
        {
            using (var state = StateProvider.BorrowJson(Settings))
            {
                var res = state.Scanner.Deserialize<T>(json);
                return res;
            }
        }
        public T FromJson<T>(FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                return FromJson<T>(fs);
        }

        public virtual T FromJson<T>(Stream stream)
        {
            var streamWrap = new StreamStreamer(stream);
            return FromJsonCharArray<T>(streamWrap);
        }

        protected virtual T FromJsonCharArray<T>(Char[] json)
        {
            using (var state = StateProvider.BorrowJson(Settings))
            {
                var res = state.Scanner.Deserialize<T>(json);
                return res;
            }
        }

        public virtual async Task<T> FromJsonAsync<T>(Stream stream)
            => await Task.Factory.StartNew(() => FromJson<T>(stream));

        public override void Dispose()
        {
        }
    }
}