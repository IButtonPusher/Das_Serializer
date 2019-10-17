using System;
using Das.Serializer;

namespace Serializer.Core.State
{
    public class BinaryBorrawable : BinaryState, IBinaryLoaner
    {
        private readonly Action<IBinaryLoaner> _returnToLibrary;

        public BinaryBorrawable(Action<IBinaryLoaner> returnToLibrary,
            ISerializerSettings settings, IStateProvider dynamicFacade)
            : base(dynamicFacade, settings)
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