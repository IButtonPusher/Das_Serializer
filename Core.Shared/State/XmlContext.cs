using System;
using Das.Serializer;

namespace Serializer.Core
{
    public class XmlContext : CoreContext, ITextContext
    {
        public XmlContext(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            

            PrimitiveScanner = new XmlPrimitiveScanner(this);
            var manipulator = new XmlNodeTypeProvider(dynamicFacade, PrimitiveScanner, settings);

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