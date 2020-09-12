using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class TextNodeSealer : BaseNodeSealer<ITextNode>
    {
        public TextNodeSealer(INodeManipulator nodeValues,
                              IStringPrimitiveScanner scanner, ISerializationCore facade,
                              ISerializerSettings settings) : base(facade, nodeValues, settings)
        {
            _values = nodeValues;
            _typeProvider = facade.NodeTypeProvider;
            _facade = facade;

            _typeManipulator = facade.TypeManipulator;
            _scanner = scanner;
            _objects = facade.ObjectManipulator;
        }

        private void BuildFromReference(ITextNode node, String refValue)
        {
            var refLength = refValue.Length;
            var chain = new Stack<ITextNode>();
            var current = node.Parent;
            while (NullNode != current)
            {
                chain.Push(current);
                current = current.Parent;
            }

            current = chain.Pop();
            var sb = new StringBuilder(current.Name);
            while (sb.Length < refLength)
            {
                current = chain.Pop();
                sb.Append($"/{current.Name}");
            }

            if (current.Value == null)
                _values.TryBuildValue(current);

            node.Value = current.Value;
            if (NullNode != node.Parent && node.Value != null)
                Imbue(node.Parent, node.Name, node.Value);
        }

        public override void CloseNode(ITextNode node)
        {
            if (node.Type == null)
                _values.InferType(node);

            if (node.NodeType == NodeTypes.None)
                node.NodeType = _typeProvider.GetNodeType(node, Settings.SerializationDepth);

            switch (node.NodeType)
            {
                case NodeTypes.Object:

                    #region object = sum of attributes (primitives) and children (should already be imbued)

                    _values.TryBuildValue(node);

                    foreach (var attr in node.Attributes)
                    {
                        if (attr.Key == DasCoreSerializer.RefTag ||
                            attr.Key == DasCoreSerializer.RefAttr)
                        {
                            BuildFromReference(node, attr.Value);
                            return;
                        }

                        var type = node.Type != null
                            ? _typeManipulator.GetPropertyType(node.Type, attr.Key)
                            : null;

                        if (type == null || node.Type == null)
                            continue;

                        var str = attr.Value;

                        var val = _scanner.GetValue(str, type) ?? str;
                        var wal = node.Value;
                        _objects.SetProperty(node.Type, attr.Key, ref wal!, val);
                    }

                    break;

                #endregion

                case NodeTypes.Collection:
                    ConstructCollection(ref node);
                    break;

                case NodeTypes.Primitive:
                    var nodeText = node.Text;
                    if (node.Type != null && !String.IsNullOrWhiteSpace(nodeText))
                        node.Value = _scanner.GetValue(nodeText, node.Type);
                    break;

                case NodeTypes.PropertiesToConstructor:
                    ConstructFromProperties(ref node);
                    break;
                case NodeTypes.Dynamic:

                    #region collate property types, generate type, set prop values

                    if (node.Type != null &&
                        _facade.TypeInferrer.IsCollection(node.Type))
                    {
                        var gt = _facade.TypeInferrer.GetGermaneType(node.Type);
                        var arr = Array.CreateInstance(gt, node.DynamicProperties.Count);
                        for (var i = 0; i < arr.Length; i++)
                            arr.SetValue(node.DynamicProperties[i.ToString()], i);

                        node.Value = arr;
                        break;
                    }

                    if (node.Value != null)
                        break;

                    foreach (var attr in node.Attributes)
                    {
                        var atVal = _scanner.GetValue(attr.Value, Const.ObjectType);
                        node.DynamicProperties.Add(attr.Key, atVal);
                    }

                    if (node.Type != null && node.Children.Count == 1)
                    {
                        var onlyChild = node.Children.First().Value;
                        if (onlyChild.Value != null &&
                            node.Type.IsInstanceOfType(onlyChild.Value))
                        {
                            Imbue(node.Parent, node.Name, onlyChild.Value);
                            return;
                        }
                    }

                    var dynamicType = _values.BuildDynamicType(node);

                    node.Type = dynamicType.ManagedType;

                    node.Value = _facade.ObjectInstantiator.BuildDefault(node.Type,
                        Settings.CacheTypeConstructors);

                    foreach (var pv in node.DynamicProperties)
                    {
                        var propName = pv.Key;
                        var value = pv.Value;
                        if (!dynamicType.IsLegalValue(propName, value))
                            if (value is String str)
                            {
                                var propType = dynamicType.GetPropertyType(propName);
                                if (propType == null)
                                    continue;

                                value = _scanner.GetValue(str, propType);
                            }

                        var wal = node.Value;
                        dynamicType.SetPropertyValue(ref wal!, propName, value);
                        node.Value = wal;
                    }

                    break;

                #endregion

                case NodeTypes.Fallback:

                    #region serialize/maybe takes string as a constructor

                    if (node.Text.Length == 0)
                    {
                        node.Value = null;
                        return;
                    }

                    var txt = node.Text;

                    var isGoodReturnType = !_facade.TypeInferrer.IsUseless(node.Type);

                    if (node.Type != null)
                        node.Value = _scanner.GetValue(txt, node.Type);

                    if (isGoodReturnType && node.Value != null)
                        Imbue(node.Parent, node.Name, node.Value);

                    break;

                #endregion
            }

            var parent = node.Parent;

            if (NullNode != parent && node.Value != null)
                Imbue(parent, node.Name, node.Value);
        }


        public override Boolean TryGetPropertyValue(ITextNode node, String key,
                                                    Type propertyType, out Object val)
        {
            var propKey = _facade.TypeInferrer.ToPropertyStyle(key);

            if (node.Attributes.TryGetValue(propKey, out var attrib))
            {
                val = _scanner.GetValue(attrib, propertyType);
                return true;
            }

            if (node.Children.TryGetValue(propKey, out var child) && child.Value != null)
            {
                val = child.Value;
                return true;
            }

            if (node.DynamicProperties.TryGetValue(propKey, out val!))
                return true;

            return false;
        }

        private static readonly NullNode NullNode = NullNode.Instance;
        private readonly ISerializationCore _facade;
        private readonly IObjectManipulator _objects;
        private readonly IStringPrimitiveScanner _scanner;
        private readonly ITypeManipulator _typeManipulator;
        private readonly INodeTypeProvider _typeProvider;
        private readonly INodeManipulator _values;
    }
}