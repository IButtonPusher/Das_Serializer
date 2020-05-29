﻿using System;


namespace Das.Serializer
{
    public class BinaryState : BaseState, IBinaryState
    {
        internal BinaryState(IStateProvider stateProvider, ISerializerSettings settings, 
            Func<IBinaryState, BinaryScanner> getScanner,
            Func<ISerializationCore, ISerializerSettings, IBinaryPrimitiveScanner> getPrimitiveScanner)
            : base(stateProvider, settings)
        {
            _settings = settings;
            PrimitiveScanner = getPrimitiveScanner(stateProvider, settings);
            _nodeProvider = stateProvider.ScanNodeProvider as IBinaryNodeProvider;

            _scanner = getScanner(this);
            Scanner = _scanner;
        }


        public IBinaryScanner Scanner { get; }

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }
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