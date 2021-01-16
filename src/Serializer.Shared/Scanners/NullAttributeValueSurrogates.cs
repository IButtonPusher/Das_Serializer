using System;

namespace Das.Serializer.Scanners
{
    public class NullAttributeValueSurrogates : IAttributeValueSurrogates
    {
        public bool TryGetValue(ITextNode node, 
                                String attributeName,
                                String attributeText, 
                                out Object value)
        {
            value = null!;
            return false;
        }

    }
}
