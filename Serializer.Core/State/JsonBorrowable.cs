using System;
using Das.Scanners;
using Das.Serializer;

namespace Serializer.Core
{
    public class JsonBorrowable : TextState, IJsonLoaner
    {
        private readonly Action<IJsonLoaner> _returnToLibrary;

        public JsonBorrowable(Action<IJsonLoaner> returnToLibrary, IStateProvider stateProvider,
            ISerializerSettings settings) 
            : base(stateProvider, stateProvider.JsonContext,  
                  new JsonScanner(stateProvider, stateProvider.JsonContext), settings)
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
