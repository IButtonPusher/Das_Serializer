using System;
using System.IO;
using System.Threading.Tasks;
using Das.Serializer.Json;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public partial class DasCoreSerializer : BaseState, IMultiSerializer
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

            JsonExpress = new JsonExpress(ObjectInstantiator, TypeManipulator, TypeInferrer);
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

        public void SetTypeSurrogate(Type looksLike, Type isReally)
        {
            Surrogates.AddOrUpdate(looksLike, isReally, (k, v) => isReally);
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

        protected JsonExpress JsonExpress;
    }
}