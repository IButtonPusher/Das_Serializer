using System;
using Das.Scanners;
using Das.Serializer;
using Serializer.Core.NodeBuilders;

namespace Serializer.Core
{
    public class BinaryContext : CoreContext, IBinaryContext
    {
        public BinaryContext(IDynamicFacade dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _nodeProvider = new BinaryNodeProvider(dynamicFacade, settings);
            PrimitiveScanner = new BinaryPrimitiveScanner(dynamicFacade, settings);
        }

        private readonly IBinaryNodeProvider _nodeProvider;

        IBinaryNodeProvider IBinaryContext.NodeProvider => _nodeProvider;

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }

        public override INodeProvider NodeProvider => _nodeProvider;
    }
}