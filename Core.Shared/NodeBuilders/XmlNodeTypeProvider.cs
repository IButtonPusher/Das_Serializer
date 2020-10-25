using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class XmlNodeTypeProvider : NodeManipulator
    {
        public XmlNodeTypeProvider(ISerializationCore dynamicFacade,
                                   IStringPrimitiveScanner scanner, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _scanner = scanner;
            _textParser = dynamicFacade.TextParser;
            _typeInferrer = dynamicFacade.TypeInferrer;
        }

        protected sealed override Boolean TryGetExplicitType(INode node, out Type type)
        {
            if (node.Attributes.TryGetValue(Const.XmlType, out var xmlType))
            {
                var str = _textParser.After(xmlType, ":");
                str = _scanner.Descape(str);

                type = _typeInferrer.GetTypeFromClearName(str)!;

                node.Attributes.Remove(Const.XmlType);
            }
            else
            {
                type = default!;
            }

            return type != null;
        }

        private readonly IStringPrimitiveScanner _scanner;
        private readonly ITextParser _textParser;
        private readonly ITypeInferrer _typeInferrer;
    }
}