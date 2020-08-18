using Das.Printers;
using System;
using System.IO;
using System.Threading.Tasks;

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

        private String ObjectToTypedXml(Object o, Type asType)
        {
            var nodeType = NodeTypeProvider.GetNodeType(asType, Settings.SerializationDepth);

            using (var writer = new StringSaver())
            {
                var settings = Settings;
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
                        ? TypeInferrer.ToClearName(asType, true) : Root;

                    
                    
                    using (var node = PrintNodePool.GetNamedValue(rootText, o, asType))
                        printer.PrintNode(node);

                    return writer.ToString();
                }
            }
        }

        public String ToXml<TObject>(TObject o)
        {
            var oType = typeof(TObject);
            return ObjectToTypedXml(o, oType);
        }

        public String ToXml<TTarget>(Object o)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o);
            return ObjectToTypedXml(obj, typeof(TTarget));
        }

        public async Task ToXml(Object o, FileInfo fi)
        {
            var xml = ToXml(o);
            await XmlToFile(xml, fi);

        }

        public async Task ToXml<TTarget>(Object o, FileInfo fi)
        {
            //var obj = ObjectManipulator.CastDynamic<TTarget>(o);
            var xml = ToXml<TTarget>(o);
            await XmlToFile(xml, fi);
        }

        public async Task ToXml<TObject>(TObject o, FileInfo fileName)
        {
            var xml = ObjectToTypedXml(o, typeof(TObject));
            await XmlToFile(xml, fileName);
        }

        private async Task XmlToFile(String xml, FileInfo fi)
        {
            using (TextWriter tr = new StreamWriter(fi.FullName))
                await _writeAsync(tr, xml);
        }
    }
}