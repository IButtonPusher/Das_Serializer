using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class BaseNodeSealer<TNode> : INodeSealer<TNode>
        where TNode : INode<TNode>
    {
        private readonly IDynamicFacade _facade;
        private readonly INodeManipulator _values;
        private readonly IObjectManipulator _objects;
        private readonly ITypeInferrer _types;

        protected BaseNodeSealer(IDynamicFacade facade, INodeManipulator values,
            ISerializerSettings settings)
        {
            _facade = facade;
            _objects = facade.ObjectManipulator;
            _values = values;
            _types = facade.TypeInferrer;
            Settings = settings;
        }

        public ISerializerSettings Settings { get; set; }

        public void Imbue(TNode node, string name, object value)
        {
            if (node == null)
                return;

            var dynProps = node.DynamicProperties;

            if (node.NodeType == NodeTypes.PropertiesToConstructor)
            {
                dynProps.Add(name, value);
                return;
            }

            if (!_values.TryBuildValue(node))
            {
                if (!dynProps.ContainsKey(name))
                    dynProps.Add(name, value);
                //todo: else adding an unknown property value multiple times...
                return;
            }

            if (_types.IsCollection(node.Type))
            {
                dynProps.Add($"{dynProps.Count}", value);
                return;
            }

            if (node.NodeType == NodeTypes.Primitive)
            {
                node.Value = value;
                return;
            }

            var wal = node.Value;
            var t = wal.GetType();
            if (_objects.SetProperty(t, name,
                ref wal, value))
            {
                node.Value = wal;
                return;
            }

            wal = node.Value;

            var propType = _facade.TypeManipulator.GetPropertyType(node.Type, name);
            _objects.TryGetPropertyValue(wal, name, out var propValue);

            if (!_types.IsCollection(propType) || propValue == null ||
                !(value is IEnumerable enumerable))
                return;

            var addDelegate = _facade.TypeManipulator.GetAdder(
                propValue as IEnumerable);

            foreach (var child in enumerable)
                addDelegate(propValue, child);
        }

        public void Imbue(TNode childNode) => Imbue(childNode.Parent, childNode.Name,
            childNode.Value);

        public abstract bool TryGetPropertyValue(TNode node, string key, 
            Type propertyType, out object val);

        public abstract void CloseNode(TNode node);

        protected void ConstructCollection(ref TNode node)
        {
            if (node.Type == null)
                return;

            var childType = _types.GetGermaneType(node.Type);

            if (node.Type.IsArray || node.Type.GetConstructor(new[] { childType }) != null)
            {
                //build via initializer if possible
                var arr2 = Array.CreateInstance(childType, node.DynamicProperties.Count);
                var i = 0;

                foreach (var child in node.DynamicProperties)
                {
                    arr2.SetValue(child.Value, i++);
                }

                if (node.Type.IsArray)
                    node.Value = arr2;
                else
                    node.Value = Activator.CreateInstance(node.Type, arr2);
            }
            else
            {
                node.Value = _facade.ObjectInstantiator.BuildDefault(node.Type,
                    Settings.CacheTypeConstructors);

                var addDelegate = _facade.TypeManipulator.GetAdder(
                    node.Value as IEnumerable);

                if (addDelegate == null)
                    return;

                foreach (var child in node.DynamicProperties)
                    addDelegate(node.Value, child.Value);
            }
        }

        protected void ConstructFromProperties(ref TNode node)
        {
            if (!_facade.ObjectInstantiator.TryGetPropertiesConstructor(
                node.Type, out var cInfo))
                return;

            var values = new List<object>();
            foreach (var conParam in cInfo.GetParameters())
            {
                if (TryGetPropertyValue(node, conParam.Name, conParam.ParameterType, out var val))
                    values.Add(val);
                
            }

            node.Value = cInfo.Invoke(values.ToArray());
        }
    }
}
