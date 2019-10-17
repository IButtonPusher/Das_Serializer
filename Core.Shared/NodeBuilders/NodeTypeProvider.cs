using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Das.Serializer;
using Das.Serializer.Scanners;

namespace Serializer.Core
{
    public class NodeTypeProvider : TypeCore, INodeManipulator
    {
        public NodeTypeProvider(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(settings)
        {
            _dynamicFacade = dynamicFacade;
            _cachedNodeTypes = new ConcurrentDictionary<Type, NodeTypes>();
            _types = dynamicFacade.TypeInferrer;
            _instantiator = dynamicFacade.ObjectInstantiator;
        }

        protected static readonly NullNode NullNode = NullNode.Instance;

        private readonly ISerializationCore _dynamicFacade;
        private readonly ConcurrentDictionary<Type, NodeTypes> _cachedNodeTypes;

        private readonly ITypeInferrer _types;
        private readonly IInstantiator _instantiator;

        protected virtual Boolean TryGetExplicitType(INode node, out Type type)
        {
            type = default;
            return false;
        }

        public IDynamicType BuildDynamicType(INode node)
        {
            var propTypes = new List<DasProperty>();
            foreach (var prop in node.DynamicProperties)
            {
                propTypes.Add(new DasProperty(prop.Key, prop.Value.GetType()));
            }

            //we have to build a type here because we only now know all the properties						
            var typeName = new StringBuilder(node.Name);
            var current = node.Parent;
            while (NullNode != current)
            {
                typeName.Insert(0, $"{current.Name}_");
                current = current.Parent;
            }

            //if this is based off an interface we need to implement that
            var parentTypes = node.Type != null ? new[] {node.Type} : new Type[0];

            var dType = _dynamicFacade.DynamicTypes.GetDynamicType(
                typeName.ToString(), propTypes.ToArray(), true, Enumerable.Empty<EventInfo>(),
                null, parentTypes);

            return dType;
        }

        public Boolean TryBuildValue(INode node)
        {
            if (node.Value != null)
                return true;

            if (node.IsForceNullValue)
            {
                node.Value = null;
                return true;
            }

            InferType(node);
            var typ = node.Type;

            if (_types.IsUseless(typ))
            {
                if (NullNode == node.Parent)
                    return false;

                typ = GetChildType(node.Parent, node);
                node.Type = typ;
            }

            if (typ == null)
                return false;

            node.Value = _instantiator.BuildDefault(node.Type, Settings.CacheTypeConstructors);
            return node.Value != null;
        }

        public Type GetChildType(INode parent, INode child)
        {
            if (parent.Type == null)
            {
                InferType(parent);
                if (parent.Type == null)
                    return null;
            }

            if (_types.IsCollection(parent.Type))
                return _types.GetGermaneType(parent.Type);

            if (parent.Type.IsValueType && parent.Type.IsGenericType &&
                NullNode != parent.Parent && parent.Parent.Value
                    is IDictionary)
            {
                if (child.Name.Equals("key", StringComparison.OrdinalIgnoreCase))
                    return parent.Type.GetGenericArguments()[0];

                if (child.Name.Equals("value", StringComparison.OrdinalIgnoreCase))
                    return parent.Type.GetGenericArguments()[1];
            }
            else if (_types.IsLeaf(parent.Type, true))
                return parent.Type;

            return _dynamicFacade.TypeManipulator.GetPropertyType(parent.Type,
                child.Name);
        }

        public void InferType(INode node)
        {
            if (!TryGetExplicitType(node, out var foundType) &&
                !_types.IsInstantiable(node.Type))
            {
                if (foundType == null && NullNode != node.Parent)
                    foundType = GetChildType(node.Parent, node);

                if (foundType == null && node.Name != Const.Empty
                    && Settings.PropertySearchDepth > TextPropertySearchDepths.ResolveByPropertyName)
                {
                    //type is null but we have a name. Try with the name as is
                    switch (Settings.PropertySearchDepth)
                    {
                        case TextPropertySearchDepths.AsTypeInLoadedModules:
                            foundType = _types.GetTypeFromLoadedModules(node.Name)
                                ?? _types.GetTypeFromLoadedModules(
                                    _types.ToPropertyStyle(node.Name));
                            break;
                        case TextPropertySearchDepths.AsTypeInNamespacesAndSystem:
                            foundType = _types.GetTypeFromClearName(node.Name);
                            break;
                    }
                }
            }

            if (foundType != null)
                node.Type = foundType;
        }

        public void EnsureNodeType(INode node, NodeTypes specified)
        {
            if (specified == NodeTypes.None)
                node.NodeType = GetNodeType(node.Type,
                    Settings.SerializationDepth);
            else
                node.NodeType = specified;
        }

        public void EnsureNodeType(INode node)
        {
            node.NodeType = GetNodeType(node.Type, Settings.SerializationDepth);
        }

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
            else if (_dynamicFacade.TypeManipulator.HasSettableProperties(type))
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
                    output = _instantiator.TryGetPropertiesConstructor(type, out _)
                        ? NodeTypes.PropertiesToConstructor
                        : NodeTypes.Dynamic;
                }
            }

            _cachedNodeTypes.TryAdd(type, output);

            return output;
        }
    }
}