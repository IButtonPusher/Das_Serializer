using System;
using System.Threading.Tasks;

namespace Das.Serializer.State
{
    public class BinaryBorrawable : BinaryState, IBinaryLoaner
    {
        public BinaryBorrawable(Action<IBinaryLoaner> returnToLibrary,
                                  ISerializerSettings settings,
                                  ISerializationCore dynamicFacade,
                                  //Func<IBinaryState, BinaryScanner> getScanner,
                                  //Func<BinaryScanner> getScanner,
                                  BinaryScanner binaryScanner,
                                  IBinaryNodeProvider binaryNodeProvider,
                                  IBinaryPrimitiveScanner primitiveScanner)
                                  //Func<ISerializationCore, ISerializerSettings, IBinaryPrimitiveScanner>
                                  //    getPrimitiveScanner)
            : base(dynamicFacade, settings, binaryScanner,
                //getScanner, 
                binaryNodeProvider, primitiveScanner)//, getPrimitiveScanner)
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
