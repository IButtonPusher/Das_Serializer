using System;
using System.Collections.Generic;
using System.Text;

namespace Das.Serializer
{
    public interface ITextNode : INode<ITextNode>, IEnumerable<ITextNode>
    {
        StringBuilder Text { get; }

        void SetText(Object value);

        new String Name { get; set; }

        void AddChild(ITextNode node);

        IDictionary<String, ITextNode> Children { get; }
    }
}