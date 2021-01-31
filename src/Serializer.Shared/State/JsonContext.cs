using System;
using System.Threading.Tasks;

namespace Das.Serializer.State
{
    public class JsonContext : CoreContext, ITextContext
    {
        public JsonContext(ISerializationCore dynamicFacade,
                           ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            var manipulator = new JsonNodeTypeProvider(dynamicFacade, settings);
            PrimitiveScanner = new JsonPrimitiveScanner(dynamicFacade.TypeInferrer);
            _nodeProvider = new TextNodeProvider(dynamicFacade, manipulator,
                dynamicFacade.NodeTypeProvider, PrimitiveScanner, settings, string.Empty);

            Sealer = new TextNodeSealer(manipulator, PrimitiveScanner,
                dynamicFacade, settings, "");
        }

        ITextNodeProvider ITextContext.ScanNodeProvider => _nodeProvider;

        public override IScanNodeProvider ScanNodeProvider => _nodeProvider;

        public INodeSealer<ITextNode> Sealer { get; }

        public IStringPrimitiveScanner PrimitiveScanner { get; }

        private readonly ITextNodeProvider _nodeProvider;
    }
}
