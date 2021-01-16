using System;
using System.IO;
using System.Threading.Tasks;
using Das.Printers;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public String ToXml(Object o)
        {
            var oType = o.GetType();
            return ObjectToTypedXml(o, oType);
        }

        public String ToXml<TObject>(TObject o)
        {
            var oType = typeof(TObject);
            return ObjectToTypedXml(o!, oType);
        }

        public String ToXml<TTarget>(Object o)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o);
            return ObjectToTypedXml(obj!, typeof(TTarget));
        }

        public async Task ToXmlAsync(Object o,
                                FileInfo fi)
        {
            var xml = ToXml(o);
            await WriteTextToFileInfoAsync(xml, fi);
        }

        public async Task ToXmlAsync<TTarget>(Object o,
                                         FileInfo fi)
        {
            var xml = ToXml<TTarget>(o);
            await WriteTextToFileInfoAsync(xml, fi);
        }

        public async Task ToXmlAsync<TObject>(TObject o,
                                         FileInfo fileName)
        {
            var xml = ObjectToTypedXml(o!, typeof(TObject));
            await WriteTextToFileInfoAsync(xml, fileName);
        }

        private String ObjectToTypedXml(Object o,
                                        Type asType)
        {
            var settings = Settings;
            var nodeType = NodeTypeProvider.GetNodeType(asType, Settings.SerializationDepth);

            using (var writer = new StringSaver())
            {
                var doCopy = true;
                var amAnonymous = IsAnonymousType(asType);

                if (nodeType == NodeTypes.PropertiesToConstructor)
                {
                    doCopy = false;
                    settings = StateProvider.ObjectConverter.Copy(settings, settings);
                    settings.TypeSpecificity = TypeSpecificity.All;
                }

                if (amAnonymous)
                {
                    if (doCopy)
                        settings = StateProvider.ObjectConverter.Copy(settings, settings);

                    settings.TypeSpecificity = TypeSpecificity.All;
                    settings.CacheTypeConstructors = false;
                }

                using (var state = StateProvider.BorrowXml(settings))
                {
                    var printer = new XmlPrinter(writer, state, settings);

                    var rootText = !asType.IsGenericType && !IsCollection(asType)
                        ? TypeInferrer.ToClearName(asType, true)
                        : Root;

                    using (var node = PrintNodePool.GetNamedValue(rootText, o, asType))
                    {
                        printer.PrintNode(node);
                    }

                    return writer.ToString();
                }
            }
        }

       
    }
}
