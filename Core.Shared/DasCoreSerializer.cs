using System;
using System.IO;
using System.Threading.Tasks;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public partial class DasCoreSerializer : BaseState, IMultiSerializer
    {
        public IStateProvider StateProvider { get; }
       

        internal const String StrNull = "null";
        internal const String Val = "__val";
        internal const String RefTag = "__ref";
        internal const String RefAttr = "$ref";
        internal const String Root = "Root";

        public override IScanNodeProvider ScanNodeProvider
            => StateProvider.BinaryContext.ScanNodeProvider;

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

        public virtual IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
            ProtoBufOptions<TPropertyAttribute> options) 
            where TPropertyAttribute : Attribute
        {
            return new ProtoBufSerializer<TPropertyAttribute>(StateProvider, Settings,
                new CoreProtoProvider());
        }

    }
}