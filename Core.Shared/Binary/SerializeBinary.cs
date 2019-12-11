using Das.Printers;
using System;
using System.IO;
using Das.Serializer;
using Serializer.Core.Files;
using Serializer.Core.Remunerators;

namespace Das
{
    public partial class DasCoreSerializer
    {
        

        public Byte[] ToBytes(Object o) => ToBytes(o, Const.ObjectType);

        public Byte[] ToBytes(Object o, Type asType)
        {
            using (var ms = new MemoryStream())
            {
                var bWriter = new BinaryWriterWrapper(ms);

                using (var state = StateProvider.BorrowBinary(Settings))
                using (var bp = new BinaryPrinter(bWriter, state))
                {
                    //var node = new NamedValueNode(Const.Root, o, asType);
                    using (var node = PrintNodePool.GetNamedValue(Const.Root, o, asType))
                        bp.PrintNode(node);
                }

                return ms.ToArray();
            }
        }

        public Byte[] ToBytes<TObject>(TObject o) => ToBytes(o, typeof(TObject));


        // ReSharper disable once UnusedMember.Global
        public Byte[] ToBytes<TTarget>(Object o)
        {
            if (ObjectManipulator.TryCastDynamic<TTarget>(o, out var obj))
                return ToBytes(obj);

            //can't actually cast it so just do property for property
            return ToBytes(o, typeof(TTarget));
        }

        public void ToBytes(Object o, FileInfo fi)
        {
            var bytes = ToBytes(o);

            using (var _ = new SafeFile(fi))
                File.WriteAllBytes(fi.FullName, bytes);
        }

        // ReSharper disable once UnusedMember.Global
        public void ToBytes<TTarget>(Object o, FileInfo fileName)
        {
            var obj = (TTarget) o;
            ToBytes(obj, fileName);
        }

     

      
    }
}