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
            PrimitiveScanner = getPrimitiveScanner(stateProvider, settings);
            _nodeProvider = stateProvider.ScanNodeProvider as IBinaryNodeProvider;

            _scanner = getScanner(this);
            Scanner = _scanner;
        }


        public IBinaryScanner Scanner { get; }

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }
        public BinaryLogger Logger { get; }

        IBinaryNodeProvider IBinaryContext.ScanNodeProvider => _nodeProvider;

        public override IScanNodeProvider ScanNodeProvider => _nodeProvider;

        private readonly BinaryScanner _scanner;
        private ISerializerSettings _settings;
        private readonly IBinaryNodeProvider _nodeProvider;

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