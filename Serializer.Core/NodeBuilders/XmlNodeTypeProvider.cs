﻿using System;
using Das.Serializer;

namespace Serializer.Core
{
    public class XmlNodeTypeProvider : NodeTypeProvider
    {
        private readonly IDynamicTypes _dynamicTypes;
        private readonly IStringPrimitiveScanner _scanner;
        private readonly ITextParser _textParser;
        private readonly ITypeInferrer _typeInferrer;

        public XmlNodeTypeProvider(IDynamicFacade dynamicFacade,
            IStringPrimitiveScanner scanner, ISerializerSettings settings) 
            : base(dynamicFacade, settings)
        {
            _dynamicTypes = dynamicFacade.DynamicTypes;
            _scanner = scanner;
            _textParser = dynamicFacade.TextParser;
            _typeInferrer = dynamicFacade.TypeInferrer;
        }

        protected override bool TryGetExplicitType(INode node, out Type type)
        {
            if (node.Attributes.TryGetValue(Const.XmlType, out var xmlType))
            {
                var str = _textParser.After(xmlType, ":");
                str = _scanner.Descape(str);
                
                type = _typeInferrer.GetTypeFromClearName(str);

                if (type == null) //we had an explicit type specified but couldn't find it??
                    _dynamicTypes.InvalidateDynamicTypes(); //not in this state

                node.Attributes.Remove(Const.XmlType);
            }
            else type = default;

            return type != null;
        }
    }
}
