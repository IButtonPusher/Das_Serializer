using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Das.Printers;
using Das.Serializer.Files;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public void ToJson(Object o, FileInfo fi)
        {
            var json = ToJson(o);

            using (var _ = new SafeFile(fi))
            {
                File.WriteAllText(fi.FullName, json);
            }
        }

        public void ToJson<TTarget>(Object o, FileInfo fileName)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o)!;
            ToJson(obj, fileName);
        }

        public String JsonEscape(String str)
        {
            using (var saver = _escapeSaver.Value!)
            {
                JsonPrinter.AppendEscaped(str, saver);
                var res = saver.ToString();

                return res;
            }
        }

        /// <summary>
        ///     Create a Json string from any object.  For more options set the Settings
        ///     property of the serializer instance or the factory on which this is invoked
        /// </summary>
        /// <param name="o">The object to serialize</param>
        public String ToJson(Object o)
        {
            return ToJson(o, o.GetType());
        }

        public String ToJson<TObject>(TObject o)
        {
            return ToJson(o!, typeof(TObject));
        }


        public String ToJson<TTarget>(Object o)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o);
            var str = ToJson(obj);

            return str;
        }

        private static StringSaver NewStringSaver()
        {
            return new StringSaver();
        }

        [MethodImpl(256)]
        private String ToJson(Object obj, Type asType)
        {
            using (var sp = _escapeSaver.Value!)
            {
                using (var state = StateProvider.BorrowJson(Settings))
                {
                    var jp = new JsonPrinter(sp, state);
                    using (var node = PrintNodePool.GetNamedValue(String.Empty, obj, asType))
                    {
                        jp.PrintNode(node);
                    }

                    var str = sp.ToString();

                    return str;
                }
            }
        }

        private static readonly ThreadLocal<StringSaver> _escapeSaver =
            new ThreadLocal<StringSaver>(NewStringSaver);
    }
}