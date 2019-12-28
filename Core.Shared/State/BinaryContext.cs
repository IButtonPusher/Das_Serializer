using Das.Serializer.Scanners;
using Das.Serializer;
using Serializer.Core.NodeBuilders;

namespace Serializer.Core
{
    public class BinaryContext : CoreContext, IBinaryContext
    {
        public BinaryContext(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _nodeProvider = new BinaryNodeProvider(dynamicFacade, settings);
            PrimitiveScanner = new BinaryPrimitiveScanner(dynamicFacade, settings);
        }

        private readonly IBinaryNodeProvider _nodeProvider;

        IBinaryNodeProvider IBinaryContext.ScanNodeProvider => _nodeProvider;

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }

        public override IScanNodeProvider ScanNodeProvider => _nodeProvider;
    }
}