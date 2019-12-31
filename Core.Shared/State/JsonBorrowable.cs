using System;
using Das.Serializer.Scanners;

namespace Das.Serializer
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