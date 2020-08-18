using System;
using System.Collections;
using System.Collections.Concurrent;


namespace Das.Serializer
{
    public class NodeTypeProvider : TypeCore, INodeTypeProvider
    {
        public NodeTypeProvider(ITypeCore typeCore, ISerializerSettings settings)
            : base(settings)
        {
            _cachedNodeTypes = new ConcurrentDictionary<Type, NodeTypes>();
            _types = typeCore;
        }

        protected static readonly NullNode NullNode = NullNode.Instance;
        
        private readonly ConcurrentDictionary<Type, NodeTypes> _cachedNodeTypes;

        private readonly ITypeCore _types;
     

       



        public NodeTypes GetNodeType(INode node, SerializationDepth depth)
            => GetNodeType(node.Type, depth);

        public NodeTypes GetNodeType(Type type, SerializationDepth depth)
        {
            if (type == null)
                return NodeTypes.Dynamic;

            if (_cachedNodeTypes.TryGetValue(type, out var output))
                return output;

            if (_types.IsLeaf(type, true))
                output = NodeTypes.Primitive;

            else if (!_types.IsInstantiable(type))
            {
                output = type == typeof(Type)
                    ? NodeTypes.Fallback
                    : NodeTypes.Dynamic;
            }

            else if (_types.IsCollection(type))
                output = NodeTypes.Collection;
            
            else if (_types.HasSettableProperties(type))
                output = NodeTypes.Object;
            
            else if (type.IsSerializable && !type.IsGenericType &&
                     !typeof(IStructuralEquatable).IsAssignableFrom(type))
                output = NodeTypes.Fallback;
            else
            {
                if (_types.TryGetNullableType(type, out _))
                    output = NodeTypes.Primitive;
                else
                {
                    output = _types.TryGetPropertiesConstructor(type, out _)
                        ? NodeTypes.PropertiesToConstructor
                        : NodeTypes.Dynamic;
                }
            }

            _cachedNodeTypes.TryAdd(type, output);

            return output;
        }


       
    }
}