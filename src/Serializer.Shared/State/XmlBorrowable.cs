using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class XmlBorrowable : TextState, IXmlLoaner
    {
        public XmlBorrowable(Action<IXmlLoaner> returnToLibrary,
                             IStateProvider stateProvider, ISerializerSettings settings)
            : base(stateProvider, stateProvider.XmlContext,
                new XmlScanner(stateProvider, stateProvider.XmlContext), settings)
        {
            _returnToLibrary = returnToLibrary;
        }

        public override void Dispose()
        {
            base.Dispose();
            _returnToLibrary(this);
        }

        private readonly Action<IXmlLoaner> _returnToLibrary;
    }
}