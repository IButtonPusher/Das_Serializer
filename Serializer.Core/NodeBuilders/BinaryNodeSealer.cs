using System;
using Das.Serializer;

namespace Serializer.Core
{
    public class BinaryNodeSealer : BaseNodeSealer<IBinaryNode>, INodeSealer<IBinaryNode>
    {
        private readonly INodeManipulator _nodeManipulator;
        private readonly IDynamicFacade _dynamicFacade;

        public BinaryNodeSealer(INodeManipulator nodeManipulator,
            IDynamicFacade dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, nodeManipulator, settings)
        {
            _nodeManipulator = nodeManipulator;
            _dynamicFacade = dynamicFacade;
        }

        public override void CloseNode(IBinaryNode node)
        {
            switch (node.NodeType)
            {
                case NodeTypes.Collection:
                    ConstructCollection(ref node);
                    break;
                case NodeTypes.PropertiesToConstructor:
                    ConstructFromProperties(ref node);
                    break;
                case NodeTypes.Object:
                    if (!_nodeManipulator.TryBuildValue(node))
                    {
                        var dynamicType = _nodeManipulator.BuildDynamicType(node);
                        node.Type = dynamicType.ManagedType;
                        _nodeManipulator.TryBuildValue(node);
                        var wal = node.Value;

                        foreach (var pv in node.DynamicProperties)
                            dynamicType.SetPropertyValue(ref wal, pv.Key, pv.Value);

                        node.Value = wal;
                    }

                    _dynamicFacade.ObjectInstantiator.OnDeserialized(node.Value,
                        Settings.SerializationDepth);

                    break;
            }

            foreach (var item in node.PendingReferences)
                item.Value = node.Value;
        }

        public override bool TryGetPropertyValue(IBinaryNode node, string key,
            Type propertyType, out object val)
        {
            var propKey = _dynamicFacade.TypeInferrer.ToPropertyStyle(key);
            return node.DynamicProperties.TryGetValue(propKey, out val);
        }
    }
}