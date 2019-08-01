using System;
using Das.Serializer;

namespace Serializer.Core
{
    public class XmlContext : CoreContext, ITextContext
    {
        public XmlContext(IDynamicFacade dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            PrimitiveScanner = new XmlPrimitiveScanner();
            var manipulator = new XmlNodeTypeProvider(dynamicFacade, PrimitiveScanner, settings);

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