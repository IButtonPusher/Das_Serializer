using Das.Serializer;
using Serializer.Core.Scanners;

namespace Serializer.Core.State
{
    public class JsonContext : CoreContext, ITextContext
    {
        public JsonContext(IDynamicFacade dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            var manipulator = new JsonNodeTypeProvider(dynamicFacade, settings);
            PrimitiveScanner = new JsonPrimitiveScanner();
            _nodeProvider = new TextNodeProvider(dynamicFacade, manipulator, PrimitiveScanner,
                settings);

            Sealer = new TextNodeSealer(manipulator, PrimitiveScanner, dynamicFacade, settings);
        }

        private readonly ITextNodeProvider _nodeProvider;
        ITextNodeProvider ITextContext.NodeProvider => _nodeProvider;
        public override INodeProvider NodeProvider => _nodeProvider;

        public INodeSealer<ITextNode> Sealer { get; }

        public IStringPrimitiveScanner PrimitiveScanner { get; }
    }
}