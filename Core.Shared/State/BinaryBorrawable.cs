using System;
using Das.Serializer.Scanners;
using Das.Serializer;

namespace Serializer.Core.State
{
    public class BinaryBorrawable : BinaryState, IBinaryLoaner
    {
        private readonly Action<IBinaryLoaner> _returnToLibrary;

        internal BinaryBorrawable(Action<IBinaryLoaner> returnToLibrary,
            ISerializerSettings settings, IStateProvider dynamicFacade,
            Func<IBinaryState, BinaryScanner> getScanner,
            Func<ISerializationCore, ISerializerSettings, IBinaryPrimitiveScanner> getPrimitiveScanner)
            : base(dynamicFacade, settings, getScanner, getPrimitiveScanner)
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