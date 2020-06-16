using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITextNode : INode<ITextNode>, IEnumerable<ITextNode>, ISerializationDepth
    {
        IDictionary<String, ITextNode> Children { get; }

        String Text { get; }

        void AddChild(ITextNode node);

        void Append(String str);

        void Append(Char c);

        void Set(String name, ISerializationDepth depth, INodeManipulator nodeManipulator);

        void SetText(Object value);
    }
}