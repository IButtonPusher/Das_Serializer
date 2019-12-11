using Das.Serializer;
using Serializer.Core.Scanners;

namespace Serializer.Core.State
{
    public class JsonContext : CoreContext, ITextContext
    {
        public JsonContext(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            var manipulator = new JsonNodeTypeProvider(dynamicFacade, settings);
            PrimitiveScanner = new JsonPrimitiveScanner(this);
            _nodeProvider = new TextNodeProvider(dynamicFacade, manipulator,
                dynamicFacade.NodeTypeProvider, PrimitiveScanner, settings);

            Sealer = new TextNodeSealer(manipulator, PrimitiveScanner, dynamicFacade, settings);
        }

        private readonly ITextNodeProvider _nodeProvider;
        ITextNodeProvider ITextContext.ScanNodeProvider => _nodeProvider;

        public override IScanNodeProvider ScanNodeProvider => _nodeProvider;

        public INodeSealer<ITextNode> Sealer { get; }

        public IStringPrimitiveScanner PrimitiveScanner { get; }
    }
}