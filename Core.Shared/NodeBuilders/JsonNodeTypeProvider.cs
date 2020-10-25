using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class JsonNodeTypeProvider : NodeManipulator
    {
        public JsonNodeTypeProvider(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _typeInferrer = dynamicFacade.TypeInferrer;
        }

        protected sealed override Boolean TryGetExplicitType(INode node, out Type type)
        {
            if (node.Attributes.TryGetValue(Const.TypeWrap, out var typeName))
            {
                type = _typeInferrer.GetTypeFromClearName(typeName)!;
                node.Attributes.Remove(Const.TypeWrap);
            }

            else
            {
                type = default!;
            }

            return type != null;
        }

        private readonly ITypeInferrer _typeInferrer;
    }
}