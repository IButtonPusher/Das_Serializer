﻿using System;
using System.Threading.Tasks;
using Das.Serializer.NodeBuilders;

namespace Das.Serializer
{
    public class BinaryContext : CoreContext, IBinaryContext
    {
        public BinaryContext(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _nodeProvider = new BinaryNodeProvider(dynamicFacade, settings);
            PrimitiveScanner = new BinaryPrimitiveScanner(dynamicFacade, settings);
        }

        IBinaryNodeProvider IBinaryContext.ScanNodeProvider => _nodeProvider;

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }

        public override IScanNodeProvider ScanNodeProvider => _nodeProvider;

        private readonly IBinaryNodeProvider _nodeProvider;
    }
}