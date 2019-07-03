﻿using System;
using System.Collections.Generic;
using Das.Scanners;
using Das.Serializer;

namespace Serializer.Core
{
    public class TextNodeProvider : NodeProvider<ITextNode>, ITextNodeProvider
    {
        public TextNodeProvider(IDynamicFacade facade, INodeManipulator nodeManipulator,
            IStringPrimitiveScanner scanner, ISerializerSettings settings) 
            : base(facade, nodeManipulator, settings)
        {
            _nodeManipulator = nodeManipulator;
            Sealer = new TextNodeSealer(TypeProvider, scanner, facade, settings);
        }

        private readonly INodeManipulator _nodeManipulator;

        public INodeSealer<ITextNode> Sealer { get; }

        public ITextNode Get(string name, Dictionary<string, string> attributes, 
            ITextNode parent)
        {
            ITextNode node;
            var buffer = Buffer;
            
            if (buffer.Count > 0)
            {
                node = buffer.Dequeue();
                node.Name = name;
            }
            node = new TextNode(name, Settings, _nodeManipulator);

            node.Parent = parent;

            foreach (var attrib in attributes)
                node.Attributes.Add(attrib.Key, attrib.Value);

            return node;
        }

      
    }
}
