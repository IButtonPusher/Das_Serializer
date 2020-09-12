﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Das.Serializer.State;

namespace Das.Serializer
{
    public class StateProvider : CoreContext, IStateProvider
    {
        public StateProvider(ISerializationCore dynamicFacade, ITextContext xmlContext,
                             ITextContext jsonContext, IBinaryContext binaryContext, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            XmlContext = xmlContext;
            JsonContext = jsonContext;
            BinaryContext = binaryContext;
            ObjectConverter = new ObjectConverter(this, settings);
        }

        public ITextContext XmlContext { get; }

        public ITextContext JsonContext { get; }

        public IBinaryContext BinaryContext { get; }

        public override IScanNodeProvider ScanNodeProvider => BinaryContext.ScanNodeProvider;

        public IObjectConverter ObjectConverter { get; }

        public IBinaryLoaner BorrowBinary(ISerializerSettings settings)
        {
            var buffer = BinaryBuffer;
            var state = buffer.Count > 0
                ? buffer.Dequeue()
                : new BinaryBorrawable(ReturnToLibrary, settings, this,
                    s => new BinaryScanner(s),
                    (c, s) => new BinaryPrimitiveScanner(c, s));
            state.UpdateSettings(settings);
            return state;
        }

        public IXmlLoaner BorrowXml(ISerializerSettings settings)
        {
            var buffer = XmlBuffer;
            var state = buffer.Count > 0
                ? buffer.Dequeue()
                : new XmlBorrowable(ReturnToLibrary, this, settings);
            state.UpdateSettings(settings);
            return state;
        }

        public IJsonLoaner BorrowJson(ISerializerSettings settings)
        {
            var buffer = JsonBuffer;
            var state = buffer.Count > 0
                ? buffer.Dequeue()
                : new JsonBorrowable(ReturnToLibrary, this, settings);
            state.UpdateSettings(settings);
            return state;
        }

        private static Queue<IBinaryLoaner> BinaryBuffer => _binaryBuffer.Value;

        private static Queue<IJsonLoaner> JsonBuffer => _jsonBuffer.Value;

        private static Queue<IXmlLoaner> XmlBuffer => _xmlBuffer.Value;

        private static void ReturnToLibrary(IBinaryLoaner loaned)
        {
            BinaryBuffer.Enqueue(loaned);
        }

        private static void ReturnToLibrary(IXmlLoaner loaned)
        {
            XmlBuffer.Enqueue(loaned);
        }

        private static void ReturnToLibrary(IJsonLoaner loaned)
        {
            JsonBuffer.Enqueue(loaned);
        }

        protected static readonly ThreadLocal<Queue<IBinaryLoaner>> _binaryBuffer
            = new ThreadLocal<Queue<IBinaryLoaner>>(() => new Queue<IBinaryLoaner>());

        protected static readonly ThreadLocal<Queue<IXmlLoaner>> _xmlBuffer
            = new ThreadLocal<Queue<IXmlLoaner>>(() => new Queue<IXmlLoaner>());

        protected static readonly ThreadLocal<Queue<IJsonLoaner>> _jsonBuffer
            = new ThreadLocal<Queue<IJsonLoaner>>(() => new Queue<IJsonLoaner>());
    }
}