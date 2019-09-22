using Das.Scanners;
using Das.Serializer;

namespace Serializer.Core
{
    public class BinaryState : BaseState, IBinaryState
    {
        public BinaryState(IStateProvider stateProvider, ISerializerSettings settings)
            : base(stateProvider, settings)
        {
            _settings = settings;
            _context = stateProvider.BinaryContext;
            PrimitiveScanner = new BinaryPrimitiveScanner(stateProvider, settings);

            _scanner = new BinaryScanner(this);
            Scanner = _scanner;
        }


        public IBinaryScanner Scanner { get; }

        IBinaryNodeProvider IBinaryContext.NodeProvider => _context.NodeProvider;

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }

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