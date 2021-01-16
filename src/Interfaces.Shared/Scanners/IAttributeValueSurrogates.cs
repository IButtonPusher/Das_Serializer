using System;

namespace Das.Serializer.Scanners
{
    public interface IAttributeValueSurrogates
    {
        Boolean TryGetValue(ITextNode node,
                            String attributeName,
                            String attributeValue,
                            out Object value);
    }
}
