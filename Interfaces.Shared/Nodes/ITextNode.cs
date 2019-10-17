using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface ITextNode : INode<ITextNode>, IEnumerable<ITextNode>, ISerializationDepth
    {
        void Set(String name, ISerializationDepth depth, INodeManipulator nodeManipulator);

        String Text { get; }

        void Append(String str);

        void Append(Char c);

        void SetText(Object value);

        void AddChild(ITextNode node);

        IDictionary<String, ITextNode> Children { get; }
    }
}