using System;
using System.IO;
using System.Threading.Tasks;
using Das.Printers;
using Das.Serializer.Remunerators;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public Byte[] ToBytes(Object o)
        {
            return ToBytes(o, Const.ObjectType);
        }

        public Byte[] ToBytes(Object o,
                              Type asType)
        {
            Byte[] bob;
            //Byte[] sally;

            using (var ms = new MemoryStream())
            {
                var bWriter = new BinaryWriterWrapper(ms);

                using (var state = StateProvider.BorrowBinary(Settings))
                using (var bp = new BinaryPrinter(bWriter, state))
                {
                    bp.PrintNode(Const.Root, asType, o);
                    //using (var node = PrintNodePool.GetNamedValue(Const.Root, o, asType))
                    //{
                    //    bp.PrintNode(node);
                    //}
                }

                bob = ms.ToArray();
            }

            //using (var ms = new MemoryStream())
            //{
            //    var bWriter = new BinaryWriterWrapper(ms);

            //    using (var state = StateProvider.BorrowBinary(Settings))
            //    using (var bp = new BinaryPrinter(bWriter, state))
            //    {
            //        //bp.PrintNode(Const.Root, asType, o);
            //        using (var node = PrintNodePool.GetNamedValue(Const.Root, o, asType))
            //        {
            //            bp.PrintNode(node);
            //        }
            //    }

            //    sally = ms.ToArray();


            //}

            //System.Diagnostics.Debug.WriteLine("old: " + string.Join(",", sally));
            //System.Diagnostics.Debug.WriteLine("new: " + string.Join(",", bob));

            return bob;
        }

        public Byte[] ToBytes<TObject>(TObject o)
        {
            return o == null
                ? throw new ArgumentNullException(nameof(o))
                : ToBytes(o, typeof(TObject));
        }


        // ReSharper disable once UnusedMember.Global
        public Byte[] ToBytes<TTarget>(Object o)
        {
            if (ObjectManipulator.TryCastDynamic<TTarget>(o, out var obj))
                return ToBytes(obj);

            //can't actually cast it so just do property for property
            return ToBytes(o, typeof(TTarget));
        }

        public void ToBytes(Object o,
                            FileInfo fi)
        {
            var bytes = ToBytes(o);

            using (var _ = new SafeFile(fi))
            {
                File.WriteAllBytes(fi.FullName, bytes);
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void ToBytes<TTarget>(Object o,
                                     FileInfo fileName)
        {
            var obj = (TTarget) o;
            ToBytes(obj!, fileName);
        }
    }
}
