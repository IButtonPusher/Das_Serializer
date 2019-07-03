using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface ITextNodeProvider : INodeProvider<ITextNode>
    {
        INodeSealer<ITextNode> Sealer { get; }

        ITextNode Get(String name, Dictionary<String, String> attributes,
            ITextNode parent);
    }
}
