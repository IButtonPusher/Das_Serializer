using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Das.Serializer.Scanners;

namespace Das.Serializer
{
    public class NodeManipulator : TypeCore, INodeManipulator
    {
        private readonly ISerializationCore _serializationCore;
        private readonly ITypeInferrer _typeInferrer;
        protected readonly IInstantiator _instantiator;
        private readonly ITypeCore _types;

        protected static readonly NullNode NullNode = NullNode.Instance;
        private INodeTypeProvider _nodeTypeProvider;

        public NodeManipulator(ISerializationCore serializationCore, ISerializerSettings settings) 
            : base(settings)
        {
            _serializationCore = serializationCore;
            _typeInferrer = serializationCore.TypeInferrer;
            _instantiator = serializationCore.ObjectInstantiator;
            _types = _typeInferrer;
            _nodeTypeProvider = serializationCore.NodeTypeProvider;
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

            var dType = _serializationCore.DynamicTypes.GetDynamicType(
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
                return _typeInferrer.GetGermaneType(parent.Type);

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

            return _serializationCore.TypeManipulator.GetPropertyType(parent.Type,
                child.Name);
        }

        protected virtual Boolean TryGetExplicitType(INode node, out Type type)
        {
            type = default;
            return false;
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
                            foundType = _typeInferrer.GetTypeFromClearName(node.Name, true)
                                ?? _typeInferrer.GetTypeFromClearName(
                                    _typeInferrer.ToPropertyStyle(node.Name), true);
                            break;
                        case TextPropertySearchDepths.AsTypeInNamespacesAndSystem:
                            foundType = _typeInferrer.GetTypeFromClearName(node.Name);
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
                node.NodeType = _nodeTypeProvider.GetNodeType(node.Type,
                    Settings.SerializationDepth);
            else
                node.NodeType = specified;
        }

        public void EnsureNodeType(INode node)
        {
            node.NodeType = _nodeTypeProvider.GetNodeType(node.Type, Settings.SerializationDepth);
        }

        
    }
}
