using System;
using System.IO;
using System.Threading.Tasks;
using Das.Serializer;
using Serializer.Core;

namespace Das
{
    public partial class DasCoreSerializer : BaseState, IMultiSerializer
    {
        protected readonly IStateProvider StateProvider;
        internal const String StrNull = "null";
        internal const String Val = "__val";
        internal const String RefTag = "__ref";
        internal const String RefAttr = "$ref";
        internal const String Root = "Root";

        public override INodeProvider NodeProvider
            => StateProvider.BinaryContext.NodeProvider;

        public DasCoreSerializer(IStateProvider stateProvider, ISerializerSettings settings,
            Func<TextWriter, String, Task> writeAsync,
            Func<TextReader, Task<String>> readToEndAsync)
            : base(stateProvider, settings)
        {
            StateProvider = stateProvider;
            _settings = settings;
            _writeAsync = writeAsync;
            _readToEndAsync = readToEndAsync;
        }

        public DasCoreSerializer(IStateProvider stateProvider, 
            Func<TextWriter, String, Task> writeAsync,
            Func<TextReader, Task<String>> readToEndAsync) 
            : this(stateProvider, stateProvider.Settings, writeAsync, readToEndAsync)
        {
        }

        public void SetTypeSurrogate(Type looksLike, Type isReally)
        {
            Surrogates.AddOrUpdate(looksLike, isReally, (k, v) => isReally);
        }

        public Boolean TryDeleteSurrogate(Type lookedLike, Type wasReally)
            => Surrogates.TryGetValue(lookedLike, out var was) && was == wasReally &&
               Surrogates.TryRemove(lookedLike, out var stillWas) && stillWas == wasReally;

        private ISerializerSettings _settings;
        private readonly Func<TextWriter, String, Task> _writeAsync;
        private readonly Func<TextReader, Task<String>> _readToEndAsync;

        public override ISerializerSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                base.Settings = value;
            }
        }
    }
}