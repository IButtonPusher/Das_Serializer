using Das.Printers;
using Das.Remunerators;
using System;
using System.IO;
using Das.Serializer;
using Serializer.Core.Files;
using Das.Serializer.Objects;

// ReSharper disable UnusedMember.Global

namespace Das
{
    public partial class DasCoreSerializer
    {
        public String ToXml(Object o)
        {
            var oType = o.GetType();
            return ToXml(o, oType);
        }

        private String ToXml(Object o, Type asType)
        {
            var nodeType = StateProvider.GetNodeType(asType, Settings.SerializationDepth);

            using (var writer = new StringSaver())
            {
                var settings = Settings;
                var doCopy = true;
                var amAnonymous = IsAnonymousType(asType);

                if (nodeType == NodeTypes.PropertiesToConstructor)
                {
                    doCopy = false;
                    settings = Copy(settings, settings);
                    settings.TypeSpecificity = TypeSpecificity.All;
                }

                if (amAnonymous)
                {
                    if (doCopy)
                        settings = Copy(settings, settings);

                    settings.TypeSpecificity = TypeSpecificity.All;
                    settings.CacheTypeConstructors = false;
                }

                using (var state = StateProvider.BorrowXml(settings))
                {
                    var printer = new XmlPrinter(writer, state, settings);

                    var rootText = !asType.IsGenericType && !IsCollection(asType) 
                        ? ToClearName(asType, true) : Root;

                    var node = new NamedValueNode(rootText, o, asType);

                    printer.PrintNode(node);

                    return writer.ToString();
                }
            }
        }

        public string ToXml<TObject>(TObject o)
        {
            var oType = typeof(TObject);
            return ToXml(o, oType);
        }

        public String ToXml<TTarget>(Object o)
        {
            var obj = CastDynamic<TTarget>(o);
            return ToXml(obj);
        }

        public void ToXml(object o, FileInfo fi)
        {
            var xml = ToXml(o);

            using (var _ = new SafeFile(fi))
            {
                File.WriteAllText(fi.FullName, xml);
            }
        }

        public void ToXml<TTarget>(object o, FileInfo fi)
        {
            var obj = CastDynamic<TTarget>(o);
            ToXml(obj, fi);
        }

        /// <summary>
        /// User friendly/less performant save to disk.  Keeps whole serialized string in memory then
        /// dumps to file when ready.  Creates the directory for the file if it doesn't
        /// already exist
        /// </summary>
        public void ToXml(Object o, string fileName)
        {
            var fi = new FileInfo(fileName);
            ToXml(o, fi);
        }
    }
}