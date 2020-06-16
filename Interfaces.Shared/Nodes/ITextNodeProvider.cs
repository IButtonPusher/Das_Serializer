using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITextNodeProvider : IScanNodeProvider<ITextNode>
    {
        INodeSealer<ITextNode> Sealer { get; }

        ITextNode Get(String name, Dictionary<String, String> attributes,
            ITextNode parent, ISerializationDepth depth);
    }
}