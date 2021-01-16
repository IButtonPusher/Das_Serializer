﻿using System;
using System.IO;
using System.Threading.Tasks;
using Das.Extensions;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public Object FromJson(String json)
        {
            #if ALWAYS_EXPRESS

            return JsonExpress.Deserialize<Object>(json, Settings, _empty);

            #else
            using (var state = StateProvider.BorrowJson(Settings))
            {
                return state.Scanner.Deserialize<Object>(json);
            }

            #endif
        }

        public T FromJson<T>(String json)
        {
            #if ALWAYS_EXPRESS

            return JsonExpress.Deserialize<T>(json, Settings, _empty);

            #else
            using (var state = StateProvider.BorrowJson(Settings))
            {
                var res = state.Scanner.Deserialize<T>(json);
                return res;
            }
            #endif
        }

        public object FromJson(String json,
                               Type type)
        {
            return JsonExpress.Deserialize(json, type, Settings, _empty);
        }

        public T FromJsonEx<T>(String json)
        {
            return JsonExpress.Deserialize<T>(json, Settings, _empty);
        }

        public T FromJsonEx<T>(String json,
                               Object[] ctorValues)
        {
            return JsonExpress.Deserialize<T>(json, Settings, ctorValues);
        }

        public T FromJson<T>(FileInfo file)
        {
            #if ALWAYS_EXPRESS
            return JsonExpress.Deserialize<T>(File.ReadAllText(file.FullName),
                Settings, _empty);
            #else
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                return FromJson<T>(fs);
            }
            #endif
        }

        public virtual T FromJson<T>(Stream stream)
        {
            #if ALWAYS_EXPRESS

            using (var sr = new StreamReader(stream))
            {
                var txt = sr.ReadToEnd();
                return JsonExpress.Deserialize<T>(txt, Settings, _empty);
            }
            #else
            var streamWrap = new StreamStreamer(stream);
            return FromJsonCharArray<T>(streamWrap);
            #endif
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
        }

        public override void Dispose()
        {
        }

        // ReSharper disable once UnusedMember.Global
        protected virtual T FromJsonCharArray<T>(Char[] json)
        {
            using (var state = StateProvider.BorrowJson(Settings))
            {
                var res = state.Scanner.Deserialize<T>(json);
                return res;
            }
        }

        private static readonly Object[] _empty = new Object[0];

        //private JsonExpress GetNewJsonExpress()
        //{
        //    return new JsonExpress(ObjectInstantiator, TypeManipulator, TypeInferrer);
        //}
    }
}


//using System;
//using System.IO;
//using System.Threading.Tasks;
//using Das.Extensions;

//namespace Das.Serializer
//{
//    public partial class DasCoreSerializer
//    {
//        public Object FromJson(String json)
//        {
//            //return JsonExpress.Deserialize(json, typeof(Object), Settings);
//            using (var state = StateProvider.BorrowJson(Settings))
//            {
//                return state.Scanner.Deserialize<Object>(json);
//            }
//        }

//        public object FromJson(String json, 
//                               Type type)
//        {
//            return JsonExpress.Deserialize(json, type, Settings);
//        }

//        public T FromJson<T>(String json)
//        {
//            //return JsonExpress.Deserialize<T>(json, Settings);

//            using (var state = StateProvider.BorrowJson(Settings))
//            {
//                var res = state.Scanner.Deserialize<T>(json);
//                return res;
//            }
//        }

//        public T FromJsonEx<T>(String json)
//        {
//            return JsonExpress.Deserialize<T>(json, Settings);
//        }

//        public T FromJson<T>(FileInfo file)
//        {
//            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
//            {
//                return FromJson<T>(fs);
//            }
//        }

//        public virtual T FromJson<T>(Stream stream)
//        {
//            var buffer = new Byte[(Int32) stream.Length - stream.Position];
//            stream.Read(buffer, 0, buffer.Length);
//            var encoding = buffer.GetEncoding();
//            //var chars = encoding.GetChars(buffer, 0, buffer.Length);
//            var json = encoding.GetString(buffer);
//            return JsonExpress.Deserialize<T>(json, Settings);

//            //var streamWrap = new StreamStreamer(stream);
//            //return FromJsonCharArray<T>(streamWrap);
//        }

//        public virtual async Task<T> FromJsonAsync<T>(Stream stream)
//        {
//            stream.Position = 0;
//            var buffer = new Byte[(Int32) stream.Length];
//            await _readAsync(stream, buffer, 0, buffer.Length);
//            var encoding = buffer.GetEncoding();
//            //var chars = encoding.GetChars(buffer, 0, buffer.Length);
//            var json = encoding.GetString(buffer);
//            return JsonExpress.Deserialize<T>(json, Settings);

//            //using (var state = StateProvider.BorrowJson(Settings))
//            //{
//            //    var res = state.Scanner.Deserialize<T>(chars);
//            //    return res;
//            //}
//        }

//        public override void Dispose()
//        {
//        }

//        //protected virtual T FromJsonCharArray<T>(Char[] json)
//        //{
//        //    using (var state = StateProvider.BorrowJson(Settings))
//        //    {
//        //        var res = state.Scanner.Deserialize<T>(json);
//        //        return res;
//        //    }
//        //}

//        //private JsonExpress GetNewJsonExpress()
//        //{
//        //    return new JsonExpress(ObjectInstantiator, TypeManipulator, TypeInferrer);
//        //}
//    }
//}
