using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class TextState : BaseState, ITextState
    {
        public TextState(IStateProvider stateProvider,
                         ITextContext context,
                         TextScanner scanner,
                         ISerializerSettings settings)
            : base(stateProvider, settings)
        {
            _context = context;

            PrimitiveScanner = _context.PrimitiveScanner;
            _scanner = scanner;
            _settings = settings;
        }

        public ITextScanner Scanner => _scanner;

        public IScannerBase<Char[]> ArrayScanner => _scanner;

        ITextNodeProvider ITextContext.ScanNodeProvider => _context.ScanNodeProvider;

        public override IScanNodeProvider ScanNodeProvider => _context.ScanNodeProvider;

        public INodeSealer<ITextNode> Sealer => _context.Sealer;

        public IStringPrimitiveScanner PrimitiveScanner { get; }

        public override void Dispose()
        {
            Scanner.Invalidate();
        }

        public override ISerializerSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                _scanner.Settings = value;
            }
        }

        private readonly ITextContext _context;

        private readonly TextScanner _scanner;

        //private ISerializerSettings _settings;
    }
}
