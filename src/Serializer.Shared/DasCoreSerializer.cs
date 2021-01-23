using System;
using System.IO;
using System.Threading.Tasks;
using Das.Serializer.Json;
using Das.Serializer.ProtoBuf;
using Das.Serializer.Xml;
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

            JsonExpress = new JsonExpress(ObjectInstantiator, TypeManipulator,
                TypeInferrer, stateProvider.ObjectManipulator, 
                stateProvider.JsonContext.PrimitiveScanner,
                DynamicTypes);

            //XmlExpress = new XmlExpress(ObjectInstantiator, TypeManipulator,
            //    _settings, stateProvider.XmlContext.PrimitiveScanner, TypeInferrer);


            XmlExpress = new XmlExpress2(ObjectInstantiator, TypeManipulator,
                stateProvider.ObjectManipulator, stateProvider.XmlContext.PrimitiveScanner,
                TypeInferrer, _settings, DynamicTypes);

            AttributeParser = new XmlPrimitiveScanner(this);
        }

        public DasCoreSerializer(IStateProvider stateProvider,
                                 Func<TextWriter, String, Task> writeAsync,
                                 Func<TextReader, Task<String>> readToEndAsync,
                                 Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : this(stateProvider, stateProvider.Settings, writeAsync, readToEndAsync, readAsync)
        {
        }

        public IStateProvider StateProvider { get; }

        public override IScanNodeProvider ScanNodeProvider
            => StateProvider.BinaryContext.ScanNodeProvider;

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

        public override ISerializerSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                base.Settings = value;
            }
        }

        public virtual IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
            ProtoBufOptions<TPropertyAttribute> options)
            where TPropertyAttribute : Attribute
        {
            return new ProtoBufSerializer(StateProvider, Settings,
                new CoreProtoProvider());
        }

        public IStringPrimitiveScanner AttributeParser { get; }


        internal const String StrNull = "null";

        internal const String Root = "Root";
        private readonly Func<Stream, Byte[], Int32, Int32, Task<Int32>> _readAsync;
        private readonly Func<TextReader, Task<String>> _readToEndAsync;
        private readonly Func<TextWriter, String, Task> _writeAsync;

        protected readonly JsonExpress JsonExpress;
        protected readonly BaseExpress XmlExpress;
        //protected readonly XmlExpress2 XmlExpress2;

        private ISerializerSettings _settings;

        protected static String GetTextFromFileInfo(FileInfo fi)
        {
            using (var _ = new SafeFile(fi))
            {
                return File.ReadAllText(fi.FullName);
            }
        }

        protected static void WriteTextToFileInfo(FileInfo fi,
                                                  String txt)
        {
            using (var _ = new SafeFile(fi))
            {
                File.WriteAllText(fi.FullName, txt);
            }
        }

        protected async Task<String> GetTextFromFileInfoAsync(FileInfo fi)
        {
            using (var _ = new SafeFile(fi))
            using (TextReader tr = new StreamReader(fi.FullName))
            {
                var res = await _readToEndAsync(tr).ConfigureAwait(true);
                return res;
            }
        }

        
        private async Task WriteTextToFileInfoAsync(String text,
                                                    FileInfo fi)
        {
            using (var _ = new SafeFile(fi))
            using (TextWriter tr = new StreamWriter(fi.FullName))
            {
                await _writeAsync(tr, text);
            }
        }

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
