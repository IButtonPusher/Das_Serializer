using System;
using Das.Scanners;
using Das.Serializer;

namespace Serializer.Core.State
{
    public class BinaryBorrawable : BinaryState, IBinaryLoaner
    {
        private readonly Action<IBinaryLoaner> _returnToLibrary;

        internal BinaryBorrawable(Action<IBinaryLoaner> returnToLibrary,
            ISerializerSettings settings, IStateProvider dynamicFacade,
            Func<IBinaryState, BinaryScanner> getScanner,
            Func<ISerializationCore, ISerializerSettings, IBinaryPrimitiveScanner> getPrimitiveScanner,
            BinaryLogger logger)
            : base(dynamicFacade, settings, getScanner, getPrimitiveScanner, logger)
        {
            _returnToLibrary = returnToLibrary;
        }

        public override void Dispose()
        {
            base.Dispose();
            _returnToLibrary(this);
        }
    }
}