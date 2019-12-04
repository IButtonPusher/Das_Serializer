using System;
using Das.Scanners;
using Das.Serializer;

namespace Serializer.Core
{
    public class BinaryState : BaseState, IBinaryState
    {
        internal BinaryState(IStateProvider stateProvider, ISerializerSettings settings, 
            Func<IBinaryState, BinaryScanner> getScanner,
            Func<ISerializationCore, ISerializerSettings, IBinaryPrimitiveScanner> getPrimitiveScanner,
            BinaryLogger logger)
            : base(stateProvider, settings)
        {
            _settings = settings;
            Logger = logger;
            _context = stateProvider.BinaryContext;
            PrimitiveScanner = getPrimitiveScanner(stateProvider, settings);

            _scanner = getScanner(this);
            Scanner = _scanner;
        }


        public IBinaryScanner Scanner { get; }

        IBinaryNodeProvider IBinaryContext.NodeProvider => _context.NodeProvider;

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }
        public BinaryLogger Logger { get; }

        public override INodeProvider NodeProvider => _context.NodeProvider;

        private readonly IBinaryContext _context;
        private readonly BinaryScanner _scanner;
        private ISerializerSettings _settings;

        public override ISerializerSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                _scanner.Settings = value;
            }
        }

        public override void Dispose()
        {
            Scanner.Invalidate();
        }
    }
}