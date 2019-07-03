using System;
using System.Collections.Generic;
using Das.Scanners;
using Das.Serializer;

namespace Serializer.Core.NodeBuilders
{
    public class BinaryNodeProvider : NodeProvider<IBinaryNode>, IBinaryNodeProvider
    {
        public BinaryNodeProvider(IDynamicFacade dynamicFacade, ISerializerSettings settings) 
            : this(dynamicFacade, new NodeTypeProvider(dynamicFacade, settings), settings)
        {
            
        }

        public BinaryNodeProvider(IDynamicFacade dynamicFacade, INodeManipulator nodeManipulator,
            ISerializerSettings settings) 
            : base(dynamicFacade, nodeManipulator, settings)
        {
            Sealer = new BinaryNodeSealer(TypeProvider, dynamicFacade, settings);
            _nodes = nodeManipulator;
        }

        public void ResolveCircularReference(IBinaryNode node, ref byte distanceFromRoot)
        {
            var chain = new Stack<IBinaryNode>();
            var current = node.Parent;
            while (current != null)
            {
                chain.Push(current);
                current = current.Parent;
            }

            current = chain.Pop();
            var index = 0;

            while (index < distanceFromRoot)
            {
                current = chain.Pop();
                index++;
            }

            TypeProvider.TryBuildValue(current);
            node.Value = current.Value;
            current.PendingReferences.Add(node);
        }

        private readonly INodeManipulator _nodes;

        public INodeSealer<IBinaryNode> Sealer { get; }

        private IBinaryNode Get(string name, Type type)
        {
            var buffer = Buffer;
            var item =  buffer.Count > 0 ? buffer.Dequeue() 
                : new BinaryNode(name, Settings);
            item.Type = type;
            return item;
        }

        public IBinaryNode Get(string name, IBinaryNode parent, Type type)
        {
            var node = Get(name, type);
            node.Parent = parent;
            _nodes.EnsureNodeType(node, node.NodeType);
            return node;
        }
    }
}
