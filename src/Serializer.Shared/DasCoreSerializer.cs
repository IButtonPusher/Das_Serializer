using System;
using System.IO;
using System.Threading.Tasks;
using Das.Serializer.Json;
using Das.Serializer.ProtoBuf;
using Das.Serializer.Xml;
using Das.Printers;

#if GENERATECODE
using Das.Serializer.Printers;
#endif

#if !NET40
using System.Runtime.CompilerServices;

#endif

namespace Das.Serializer
{
    public partial class DasCoreSerializer : BaseState,
                                             IMultiSerializer
    {
        public DasCoreSerializer(IStateProvider stateProvider,
                                 ISerializerSettings settings,
                                 Func<TextWriter, String, Task> writeAsync,
                                 Func<TextReader, Task<String>> readToEndAsync,
                                 Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : base(stateProvider, settings)
        {
            StateProvider = stateProvider;

            _settings = settings;
            _writeAsync = writeAsync;
            _readToEndAsync = readToEndAsync;
            _readAsync = readAsync;

            var jsonPrimitiveScanner = new JsonPrimitiveScanner(TypeInferrer);
            var xmlPrimitiveScanner = new XmlPrimitiveScanner(this);

            JsonExpress = new JsonExpress(ObjectInstantiator, TypeManipulator,
                TypeInferrer, stateProvider.ObjectManipulator,
                jsonPrimitiveScanner,
                DynamicTypes);

            _jsonPrinter = new JsonPrinter(
                StateProvider.TypeInferrer, StateProvider.NodeTypeProvider,
                StateProvider.ObjectManipulator, StateProvider.TypeManipulator);


            XmlExpress = new XmlExpress2(ObjectInstantiator, TypeManipulator,
                stateProvider.ObjectManipulator,
                xmlPrimitiveScanner,
                TypeInferrer, _settings, DynamicTypes);

            AttributeParser = new XmlPrimitiveScanner(this);

            #if GENERATECODE

            _proxyProvider = new DynamicPrinterProvider(TypeInferrer, StateProvider.NodeTypeProvider,
                TypeManipulator, StateProvider.ObjectInstantiator);

            #endif
        }

      
        public IStateProvider StateProvider { get; }


        public void SetTypeSurrogate(Type looksLike,
                                     Type isReally)
        {
            Surrogates[looksLike] = isReally;
        }

        public Boolean TryDeleteSurrogate(Type lookedLike,
                                          Type wasReally)
        {
            return Surrogates.TryGetValue(lookedLike, out var was) && was == wasReally &&
                   Surrogates.TryRemove(lookedLike, out var stillWas) && stillWas == wasReally;
        }

        public virtual IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
            IProtoBufOptions<TPropertyAttribute> options)
            where TPropertyAttribute : Attribute
        {
            return new ProtoBufSerializer(StateProvider, Settings,
                new CoreProtoProvider());
        }

        public IStringPrimitiveScanner AttributeParser { get; }

        public override ISerializerSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                base.Settings = value;
            }
        }

        protected static String GetTextFromFileInfo(FileInfo fi)
        {
            //using (var _ = new SafeFile(fi))
            {
                return File.ReadAllText(fi.FullName);
            }
        }

        protected async Task<String> GetTextFromFileInfoAsync(FileInfo fi)
        {
            //using (var _ = new SafeFile(fi))
            using (TextReader tr = new StreamReader(fi.FullName))
            {
                var res = await _readToEndAsync(tr).ConfigureAwait(true);
                return res;
            }
        }

        protected static void WriteTextToFileInfo(FileInfo fi,
                                                  String txt)
        {
            //using (var _ = new SafeFile(fi))
            {
                File.WriteAllText(fi.FullName, txt);
            }
        }


        private async Task WriteTextToFileInfoAsync(String text,
                                                    FileInfo fi)
        {
            //using (var _ = new SafeFile(fi))
            using (TextWriter tr = new StreamWriter(fi.FullName))
            {
                await _writeAsync(tr, text);
            }
        }


        internal const String StrNull = "null";

        internal const String Root = "Root";
        private readonly Func<Stream, Byte[], Int32, Int32, Task<Int32>> _readAsync;
        private readonly Func<TextReader, Task<String>> _readToEndAsync;
        private readonly Func<TextWriter, String, Task> _writeAsync;

        protected readonly JsonExpress JsonExpress;
        protected readonly BaseExpress XmlExpress;

        #if !NET40

        [MethodImpl(256)]
        protected static Task WriteAsync(TextWriter writer,
                                         String writeThis)
        {
            return writer.WriteAsync(writeThis);
        }

        [MethodImpl(256)]
        protected static Task<String> ReadToEndAsync(TextReader reader)
        {
            return reader.ReadToEndAsync();
        }

        [MethodImpl(256)]
        protected static Task<Int32> ReadAsync(Stream stream,
                                               Byte[] buffer,
                                               Int32 offset,
                                               Int32 count)
        {
            return stream.ReadAsync(buffer, offset, count);
        }

        #endif
    }
}
