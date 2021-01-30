using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class XmlContext : CoreContext, ITextContext
    {
        public XmlContext(ISerializationCore dynamicFacade,
                          ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            PrimitiveScanner = new XmlPrimitiveScanner(dynamicFacade.TypeInferrer);
            var manipulator = new XmlNodeTypeProvider(dynamicFacade, PrimitiveScanner, settings);

            _nodeProvider = new TextNodeProvider(dynamicFacade, manipulator,
                dynamicFacade.NodeTypeProvider, PrimitiveScanner, settings, "/");

            Sealer = new TextNodeSealer(manipulator, PrimitiveScanner, dynamicFacade, 
                settings, "/");
        }


        ITextNodeProvider ITextContext.ScanNodeProvider => _nodeProvider;

        public override IScanNodeProvider ScanNodeProvider => _nodeProvider;

        public INodeSealer<ITextNode> Sealer { get; }

        public IStringPrimitiveScanner PrimitiveScanner { get; }

        private readonly ITextNodeProvider _nodeProvider;
    }
}
