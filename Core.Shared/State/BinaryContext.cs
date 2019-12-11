using Das.Scanners;
using Das.Serializer;
using Serializer.Core.NodeBuilders;

namespace Serializer.Core
{
    public class BinaryContext : CoreContext, IBinaryContext
    {
        public BinaryContext(ISerializationCore dynamicFacade, ISerializerSettings settings, BinaryLogger logger)
            : base(dynamicFacade, settings)
        {
            _nodeProvider = new BinaryNodeProvider(dynamicFacade, settings);
            PrimitiveScanner = new BinaryPrimitiveScanner(dynamicFacade, settings);
            Logger = logger;
        }

        private readonly IBinaryNodeProvider _nodeProvider;

        IBinaryNodeProvider IBinaryContext.ScanNodeProvider => _nodeProvider;

        public IBinaryPrimitiveScanner PrimitiveScanner { get; }
        public BinaryLogger Logger { get; }

        public override IScanNodeProvider ScanNodeProvider => _nodeProvider;
    }
}