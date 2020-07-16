using Das.Printers;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Das.Serializer.Files;
using System.Threading;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public void ToJson(Object o, FileInfo fi)
        {
            var json = ToJson(o);

            using (var _ = new SafeFile(fi))
                File.WriteAllText(fi.FullName, json);
        }

        public void ToJson<TTarget>(Object o, FileInfo fileName)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o)!;
            ToJson(obj, fileName);
        }

        private static readonly ThreadLocal<StringSaver> _escapeSaver = 
            new ThreadLocal<StringSaver>(NewStringSaver);

        private static StringSaver NewStringSaver() => new StringSaver();

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
        /// Create a Json string from any object.  For more options set the Settings
        /// property of the serializer instance or the factory on which this is invoked
        /// </summary>
        /// <param name="o">The object to serialize</param>
        public String ToJson(Object o) => ToJson(o, o.GetType());

        public String ToJson<TObject>(TObject o) => ToJson(o!, typeof(TObject));

        [MethodImpl(256)]
        private String ToJson(Object obj, Type asType)
        {
            using (var sp = _escapeSaver.Value!)
            {
                using (var state = StateProvider.BorrowJson(Settings))
                {
                    var jp = new JsonPrinter(sp, state);
                    using (var node = PrintNodePool.GetNamedValue(String.Empty, obj, asType))
                        jp.PrintNode(node);

                    var str = sp.ToString();

                    return str;
                }
            }
        }


        public String ToJson<TTarget>(Object o)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o);
            var str = ToJson(obj);

            return str;
        }
    }
}