using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Das.Printers;
using Das.Serializer.Remunerators;

#if GENERATECODE
using Das.Serializer.Printers;
#endif

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
                JsonPrinter.AppendEscaped(saver, str);
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
            return new CompactStringSaver();
        }

        [MethodImpl(256)]
        private String ToJson(Object obj,
                              Type asType)
        {
            using (var sp = GetTextWriter(Settings))
            {
                _jsonPrinter.PrintObject(obj, asType, 
                    StateProvider.NodeTypeProvider.GetNodeType(asType),
                    sp, Settings, GetCircularReferenceHandler(Settings));

                return sp.ToString();
            }
        }

        public String ToJsonEx<TObject>(TObject obj,
                                        ISerializerSettings settings)
        {
            #if GENERATECODE

            using (var sp = GetTextWriter(settings))
            {
                var proxy = _proxyProvider.GetJsonProxy<TObject>(settings);
        
                //_proxyProvider.DumpProxies();
                
                proxy.Print(obj, sp);
                return sp.ToString();
            }

            #else

            return obj == null
                ? string.Empty
                : ToJson(obj, obj.GetType());

            #endif
        }

        [MethodImpl(256)]
        public String ToJsonEx<TObject>(TObject obj) => ToJsonEx(obj, _settings);

        private static readonly ThreadLocal<StringSaver> _escapeSaver = new(NewStringSaver);
        protected readonly JsonPrinter _jsonPrinter;

        #if GENERATECODE

        private readonly DynamicPrinterProvider _proxyProvider;

        #endif
    }
}
