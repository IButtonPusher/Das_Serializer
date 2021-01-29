using System;
using System.Threading.Tasks;

namespace Das.Serializer.State
{
    public class BinaryBorrawable : BinaryState, IBinaryLoaner
    {
        internal BinaryBorrawable(Action<IBinaryLoaner> returnToLibrary,
                                  ISerializerSettings settings,
                                  IStateProvider dynamicFacade,
                                  Func<IBinaryState, BinaryScanner> getScanner,
                                  Func<ISerializationCore, ISerializerSettings, IBinaryPrimitiveScanner>
                                      getPrimitiveScanner)
            : base(dynamicFacade, settings, getScanner, getPrimitiveScanner)
        {
            _returnToLibrary = returnToLibrary;
        }

        public override void Dispose()
        {
            base.Dispose();
            _returnToLibrary(this);
        }

        private readonly Action<IBinaryLoaner> _returnToLibrary;
    }
}
