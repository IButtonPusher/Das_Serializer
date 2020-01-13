using Das.Serializer.Remunerators;
using System;
using System.IO;
using System.Threading;
using Das.Printers;
using Das.Serializer.Scanners;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoBufSerializer<TPropertyAttribute> : CoreContext, IProtoSerializer
        where  TPropertyAttribute : Attribute
    {
        public ProtoBufSerializer(IStateProvider stateProvider, ISerializerSettings settings,
            ProtoBufOptions<TPropertyAttribute> options) : base(stateProvider, settings)
        {
            var options1 = options;
            StateProvider = stateProvider;

            Printer = new ThreadLocal<ProtoPrinter<TPropertyAttribute>>(() =>
                {
                    var pWriter = new ProtoBufWriter3(100);
                    var state = StateProvider.BorrowBinary(Settings);
                    var printer = new ProtoPrinter<TPropertyAttribute>(pWriter,
                        state, TypeManipulator, options1);
                    return printer;
                });

            Printer2 = new ThreadLocal<IProtoPrinter>(() =>
            {
                var pWriter = new ProtoBufWriter3(100);
                var state = StateProvider.BorrowBinary(Settings);
                var printer = new ProtoPrinter<TPropertyAttribute>(pWriter,
                    state, TypeManipulator, options1);
                return printer;
            });

            Scanner = new ThreadLocal<ProtoScanner<TPropertyAttribute>>(() =>
            {
                var state = StateProvider.BorrowProto(Settings, options1);
                var arr = new ByteStream();
                var f = new ProtoFeeder(state.PrimitiveScanner, state, arr, Settings);
                var s = (ProtoScanner<TPropertyAttribute>)state.Scanner;
                s.Feeder = f;
                return s;
            });
        }

        public IStateProvider StateProvider { get; }

        private readonly ThreadLocal<ProtoPrinter<TPropertyAttribute>> Printer;

        private readonly ThreadLocal<IProtoPrinter> Printer2;

        private readonly ThreadLocal<ProtoScanner<TPropertyAttribute>> Scanner;

        public void ToProtoStream<TObject>(Stream stream, TObject o)
        {
            var pickMe = Printer2.Value;
            pickMe.Stream = stream;
            pickMe.Print(o);
        }

        public TObject FromProtoStream<TObject>(Stream stream)
        {
            var pickMe = Scanner.Value;
            return pickMe.Deserialize<TObject>(stream);
        }

        public override IScanNodeProvider ScanNodeProvider
            => StateProvider.BinaryContext.ScanNodeProvider;
    }
}
