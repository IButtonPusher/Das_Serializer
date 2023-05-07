using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public sealed class BinaryNodeSealer : BaseNodeSealer<IBinaryNode>
{
   public BinaryNodeSealer(INodeManipulator nodeManipulator,
                           ISerializationCore dynamicFacade,
                           ISerializerSettings settings)
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
               {
                  dynamicType.SetPropertyValue(ref wal!, pv.Key, pv.Value);
               }

               node.Value = wal;
            }

            _dynamicFacade.ObjectInstantiator.OnDeserialized(node, Settings);

            break;
      }

      foreach (var item in node.PendingReferences)
      {
         item.Value = node.Value;
      }
   }

   public override Boolean TryGetPropertyValue(IBinaryNode node,
                                               String key,
                                               Type propertyType,
                                               out Object val)
   {
      var propKey = _dynamicFacade.TypeInferrer.ToPascalCase(key);
      return node.DynamicProperties.TryGetValue(propKey, out val!);
   }

   private readonly ISerializationCore _dynamicFacade;
   private readonly INodeManipulator _nodeManipulator;
}