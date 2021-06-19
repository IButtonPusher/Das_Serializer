using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer.NodeBuilders
{
    public class BinaryNodeProvider : NodeProvider<IBinaryNode>, IBinaryNodeProvider
    {
        public BinaryNodeProvider(ISerializationCore dynamicFacade,
                                  ISerializerSettings settings)
            : this(dynamicFacade, new NodeManipulator(dynamicFacade, settings),
                dynamicFacade.NodeTypeProvider, settings)
        {
        }

        public BinaryNodeProvider(ISerializationCore dynamicFacade,
                                  INodeManipulator nodeManipulator,
                                  INodeTypeProvider nodeTypes,
                                  ISerializerSettings settings)
            : base(nodeTypes, settings)
        {
            Sealer = new BinaryNodeSealer(nodeManipulator, dynamicFacade, settings);
            _nodes = nodeManipulator;
        }

        public void ResolveCircularReference(IBinaryNode node,
                                             ref Byte distanceFromRoot)
        {
            var chain = new Stack<IBinaryNode>();
            var current = node.Parent;
            while (NullNode.Instance != current)
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

            _nodes.TryBuildValue(current);
            node.Value = current.Value;
            current.PendingReferences.Add(node);
        }

        public INodeSealer<IBinaryNode> Sealer { get; }

        public IBinaryNode Get(String name,
                               IBinaryNode parent,
                               Type type)
        {
            var node = Get(name, type);
            node.Parent = parent;
            _nodes.EnsureNodeType(node, node.NodeType);
            return node;
        }

        private IBinaryNode Get(String name,
                                Type type)
        {
            var buffer = Buffer;
            var item = buffer.Count > 0
                ? buffer.Dequeue()
                : new BinaryNode(name, Settings);
            item.Type = type;
            return item;
        }

        private readonly INodeManipulator _nodes;
    }
}
