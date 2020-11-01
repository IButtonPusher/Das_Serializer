using System;
using System.IO;
#if !NET40
using System.Runtime.CompilerServices;
#endif

using System.Threading.Tasks;
using Das.Serializer.Json;
using Das.Serializer.ProtoBuf;
using Das.Serializer.Xml;

namespace Das.Serializer
{
    public partial class DasCoreSerializer : BaseState, IMultiSerializer
    {
        #if !NET40

        [MethodImpl(256)]
        protected static Task WriteAsync(TextWriter writer, String writeThis)
            => writer.WriteAsync(writeThis);

        [MethodImpl(256)]
        protected static Task<String> ReadToEndAsync(TextReader reader)
            => reader.ReadToEndAsync();

        [MethodImpl(256)]
        protected static Task<Int32> ReadAsync(Stream stream, Byte[] buffer, Int32 offset, Int32 count)
            => stream.ReadAsync(buffer, offset, count);

        #endif

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

            JsonExpress = new JsonExpress(ObjectInstantiator, TypeManipulator, TypeInferrer);
            XmlExpress = new XmlExpress(ObjectInstantiator, TypeManipulator,
                _settings, StateProvider.XmlContext.PrimitiveScanner);
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

        public Boolean TryDeleteSurrogate(Type lookedLike, Type wasReally)
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


        internal const String StrNull = "null";

        internal const String RefTag = "__ref";
        internal const String RefAttr = "$ref";
        internal const String Root = "Root";
        private readonly Func<Stream, Byte[], Int32, Int32, Task<Int32>> _readAsync;
        private readonly Func<TextReader, Task<String>> _readToEndAsync;
        private readonly Func<TextWriter, String, Task> _writeAsync;

        private ISerializerSettings _settings;

        protected readonly JsonExpress JsonExpress;
        protected readonly XmlExpress XmlExpress;
    }
}