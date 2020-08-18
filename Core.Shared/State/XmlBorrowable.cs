﻿using System;


namespace Das.Serializer
{
    public class XmlBorrowable : TextState, IXmlLoaner
    {
        private readonly Action<IXmlLoaner> _returnToLibrary;

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
    }
}