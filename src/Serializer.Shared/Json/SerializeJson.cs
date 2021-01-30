using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Das.Printers;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public void ToJson(Object o,
                           FileInfo fileInfo)
        {
            var json = ToJson(o);

            WriteTextToFileInfo(fileInfo, json);
        }

        public void ToJson<TTarget>(Object o,
                                    FileInfo fileInfo)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o)!;
            ToJson(obj, fileInfo);
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
            return new();
        }

        [MethodImpl(256)]
        private String ToJson(Object obj,
                              Type asType)
        {
            using (var sp = _escapeSaver.Value!)
            {
                using (var state = StateProvider.BorrowJson(Settings))
                {
                    var jp = new JsonPrinter(sp, state);

                    //String str1, str2;

                    //using (var node = PrintNodePool.GetNamedValue(String.Empty, obj, asType))
                    {
                        //jp.PrintNode(node);
                        //var str1 = sp.ToString();
                        //sp.Clear();


                        jp.PrintNode(string.Empty, asType, obj);
                            //NodeTypeProvider.GetNodeType(asType, _settings.SerializationDepth));
                    }

                    return sp.ToString();
                    //str2 = sp.ToString();

                    //if (str1 != str2)
                    //{}

                    //return str2;
                }
            }
        }

        private static readonly ThreadLocal<StringSaver> _escapeSaver =
            new(NewStringSaver);
    }
}
