using System;
using System.Collections.Generic;
using System.Text;

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
