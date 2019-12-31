using System;
using System.Collections.Generic;
using Das.Serializer.Scanners;

namespace Das.Serializer
{
    public class TextNodeProvider : NodeProvider<ITextNode>, ITextNodeProvider
    {
        public TextNodeProvider(ISerializationCore facade, INodeManipulator nodeManipulator,
            INodeTypeProvider nodeTypes,
            IStringPrimitiveScanner scanner, ISerializerSettings settings)
            : base(facade.NodeTypeProvider, settings)
        {
            _nodeManipulator = nodeManipulator;
            _nodeTypes = nodeTypes;
            Sealer = new TextNodeSealer(nodeManipulator, scanner, facade, settings);
        }

        private readonly INodeManipulator _nodeManipulator;
        private readonly INodeTypeProvider _nodeTypes;

        public INodeSealer<ITextNode> Sealer { get; }

        public ITextNode Get(String name, Dictionary<String, String> attributes,
            ITextNode parent, ISerializationDepth depth)
        {
            ITextNode node;
            var buffer = Buffer;

            if (buffer.Count > 0)
            {
                node = buffer.Dequeue();
                node.Set(name, depth, _nodeManipulator);
            }
            else
                node = new TextNode(name, Settings, _nodeManipulator, _nodeTypes, depth);
            

            node.Parent = parent;

            foreach (var attrib in attributes)
                node.Attributes.Add(attrib.Key, attrib.Value);

            return node;
        }
    }
}