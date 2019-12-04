using Das.Printers;
using System;
using System.IO;
using Das.Serializer;
using Serializer.Core.Files;
using Das.Serializer.Objects;
using Das.Serializer.Remunerators;
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
                    var node = new NamedValueNode(Const.Root, o, asType);
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

        public void ToProtoStream<TObject, TPropertyAttribute>(Stream stream, TObject o,
            ProtoBufOptions<TPropertyAttribute> options)
            where TPropertyAttribute : Attribute
        {
            var pWriter = new ProtoBufWriter(stream);
            using (var state = StateProvider.BorrowBinary(Settings))
            using (var printer = new ProtoPrinter<TPropertyAttribute>(pWriter, 
                state, TypeManipulator, options))
            {
                
                var node = new NamedValueNode(Const.Root, o, typeof(TObject));
                printer.PrintNode(node);
            }
        }

      
    }
}