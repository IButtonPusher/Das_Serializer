using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class JsonBorrowable : TextState, IJsonLoaner
    {
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

        private readonly Action<IJsonLoaner> _returnToLibrary;
    }
}