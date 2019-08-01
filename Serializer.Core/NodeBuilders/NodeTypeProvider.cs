using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Das.Serializer;

namespace Serializer.Core
{
    public class NodeTypeProvider : TypeCore, INodeManipulator
    {
        public NodeTypeProvider(IDynamicFacade dynamicFacade, ISerializerSettings settings)
            : base(settings)
        {
            _dynamicFacade = dynamicFacade;
            _cachedNodeTypes = new ConcurrentDictionary<Type, NodeTypes>();
            _types = dynamicFacade.TypeInferrer;
            _instantiator = dynamicFacade.ObjectInstantiator;
        }

        private readonly IDynamicFacade _dynamicFacade;
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
            while (current != null)
            {
                typeName.Insert(0, $"{current.Name}_");
                current = current.Parent;
            }

            //if this is based off an interface we need to implement that
            Type[] parentTypes;
            if (node.Type != null)
                parentTypes = new[] {node.Type};
            else
                parentTypes = new Type[0];

            var dType = _dynamicFacade.DynamicTypes.GetDynamicType(
                typeName.ToString(), propTypes.ToArray(), true,
                null, parentTypes);

            return dType;
        }

        public bool TryBuildValue(INode node)
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
                if (node.Parent == null)
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

            TryBuildValue(parent);
            if (_types.IsCollection(parent.Type))
                return _types.GetGermaneType(parent.Type);

            return _dynamicFacade.TypeManipulator.GetPropertyType(parent.Type,
                child.Name);
        }

        public void InferType(INode node)
        {
            if (!TryGetExplicitType(node, out var foundType) &&
                !_types.IsInstantiable(node.Type))
            {
                if (foundType == null && node.Parent != null)
                    foundType = GetChildType(node.Parent, node);

                if (foundType == null && node.Name != null)
                {
                    //type is null but we have a name. Try with the name as is
                    foundType = _types.GetTypeFromClearName(node.Name);

                    if (foundType == null)
                    {
                        var search = _types.ToPropertyStyle(node.Name);
                        foundType = _types.GetTypeFromClearName(search);
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
            else if (_dynamicFacade.TypeManipulator.PropertyCount(type) > 0)
                output = NodeTypes.Object;
            else if (_types.IsCollection(type))
                output = NodeTypes.Collection;
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