﻿using System;
using Das.Scanners;
using Das.Serializer;

namespace Serializer.Core
{
    public class TextState : BaseState, ITextState
    {
        public TextState(IStateProvider stateProvider,
            ITextContext context, TextScanner scanner, ISerializerSettings settings)
            : base(stateProvider, settings)
        {
            _context = context;

            PrimitiveScanner = _context.PrimitiveScanner;
            _scanner = scanner;
        }

        public ITextScanner Scanner => _scanner;
        public IScannerBase<Char[]> ArrayScanner => _scanner;

        private readonly TextScanner _scanner;

        ITextNodeProvider ITextContext.NodeProvider => _context.NodeProvider;
        public override INodeProvider NodeProvider => _context.NodeProvider;

        public INodeSealer<ITextNode> Sealer => _context.Sealer;

        public IStringPrimitiveScanner PrimitiveScanner { get; }

        private readonly ITextContext _context;

        public override void Dispose()
        {
            Scanner.Invalidate();
        }

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
    }
}