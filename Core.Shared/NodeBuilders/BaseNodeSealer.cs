using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public abstract class BaseNodeSealer<TNode> : INodeSealer<TNode>
        where TNode : INode<TNode>, INode
    {
        protected BaseNodeSealer(ISerializationCore facade, INodeManipulator values,
                                 ISerializerSettings settings)
        {
            _facade = facade;
            _objects = facade.ObjectManipulator;
            _values = values;
            _types = facade.TypeInferrer;
            Settings = settings;
        }

        public ISerializerSettings Settings { get; }

        public void Imbue(TNode node, String name, Object value)
        {
            if (NullNode.Instance == node)
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

            if (node.Type != null && _types.IsCollection(node.Type))
            {
                dynProps.Add($"{dynProps.Count}", value);
                return;
            }

            if (node.NodeType == NodeTypes.Primitive)
            {
                node.Value = value;
                return;
            }

            var wal = node.Value ?? throw new NullReferenceException(node.ToString());
            var t = wal.GetType();
            if (_objects.SetProperty(t, name, ref wal, value)) return;

            wal = node.Value;

            var propType = _facade.TypeManipulator.GetPropertyType(node.Type!, name);
            _objects.TryGetPropertyValue(wal, name, out var propValue);

            if (propType == null || !_types.IsCollection(propType) || propValue == null ||
                !(value is IEnumerable enumerable))
                return;

            var addDelegate = _facade.TypeManipulator.GetAdder(
                (propValue as IEnumerable)!);

            foreach (var child in enumerable)
                addDelegate(propValue, child);
        }

        public void Imbue(TNode childNode)
        {
            Imbue(childNode.Parent, childNode.Name,
                childNode.Value!);
        }

        public abstract void CloseNode(TNode node);

        protected void ConstructCollection(ref TNode node)
        {
            if (node.Type == null || node.IsEmpty)
                return;

            var childType = _types.GetGermaneType(node.Type);

            if (node.Type.IsArray)
            {
                var arr2 = Array.CreateInstance(childType, node.DynamicProperties.Count);
                var i = 0;

                foreach (var child in node.DynamicProperties)
                    arr2.SetValue(child.Value, i++);

                node.Value = arr2;

                return;
            }

            var ctorArg = typeof(IEnumerable<>).MakeGenericType(childType);

            var ctor = node.Type.GetConstructor(new[] {ctorArg});

            if (ctor != null)
            {
                //build via initializer if possible
                var arr2 = Array.CreateInstance(childType, node.DynamicProperties.Count);
                var i = 0;

                foreach (var child in node.DynamicProperties)
                    arr2.SetValue(child.Value, i++);

                node.Value = Activator.CreateInstance(node.Type, arr2);
            }
            else
            {
                node.Value = _facade.ObjectInstantiator.BuildDefault(node.Type,
                    Settings.CacheTypeConstructors);


                if (node.DynamicProperties.Count == 0)
                    return;

                var addDelegate = _facade.TypeManipulator.GetAdder(node.Type,
                    node.DynamicProperties.Values.First());
                if (addDelegate == null && node.Value is IEnumerable ienum)
                    addDelegate = _facade.TypeManipulator.GetAdder(ienum);


                if (addDelegate == null)
                    return;

                foreach (var child in node.DynamicProperties)
                    addDelegate(node.Value!, child.Value);
            }
        }

        protected void ConstructFromProperties(ref TNode node)
        {
            if (node.Type == null || !_facade.ObjectInstantiator.TryGetPropertiesConstructor(
                node.Type, out var cInfo))
                return;

            var values = new List<Object>();
            foreach (var conParam in cInfo.GetParameters())
                if (TryGetPropertyValue(node, conParam.Name, conParam.ParameterType, out var val))
                    values.Add(val);

            node.Value = cInfo.Invoke(values.ToArray());
        }

        public abstract Boolean TryGetPropertyValue(TNode node, String key,
                                                    Type propertyType, out Object val);

        private readonly ISerializationCore _facade;
        private readonly IObjectManipulator _objects;
        private readonly ITypeInferrer _types;
        private readonly INodeManipulator _values;
    }
}