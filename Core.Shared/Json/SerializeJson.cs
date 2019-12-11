using Das.Printers;
using Das.Remunerators;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Serializer.Core.Files;

namespace Das
{
    public partial class DasCoreSerializer
    {
        public void ToJson(Object o, FileInfo fi)
        {
            var xml = ToJson(o);

            using (var _ = new SafeFile(fi))
                File.WriteAllText(fi.FullName, xml);
        }

        public void ToJson<TTarget>(Object o, FileInfo fileName)
        {
            var obj = ObjectManipulator.CastDynamic<TTarget>(o);
            ToJson(obj, fileName);
        }

        /// <summary>
        /// Create a Json string from any object.  For more options set the Settings
        /// property of the serializer instance or the factory on which this is invoked
        /// </summary>
        /// <param name="o">The object to serialize</param>
        public String ToJson(Object o) => ToJson(o, o.GetType());

        public String ToJson<TObject>(TObject o) => ToJson(o, typeof(TObject));

        [MethodImpl(256)]
        private String ToJson(Object obj, Type asType)
        {
            using (var sp = new StringSaver())
            {
                using (var state = StateProvider.BorrowJson(Settings))
                {
                    var jp = new JsonPrinter(sp, state);
                    using (var node = PrintNodePool.GetNamedValue(String.Empty, obj, asType))
                        jp.PrintNode(node);
                    //var node = new NamedValueNode(String.Empty, obj, asType);
                    
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