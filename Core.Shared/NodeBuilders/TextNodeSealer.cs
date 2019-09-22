using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Das;
using Das.Serializer;

namespace Serializer.Core
{
    public class TextNodeSealer : BaseNodeSealer<ITextNode>
    {
        private readonly INodeManipulator _values;
        private readonly INodeManipulator _typeProvider;
        private readonly IDynamicFacade _facade;
        private readonly IStringPrimitiveScanner _scanner;
        private readonly ITypeManipulator _typeManipulator;
        private readonly IObjectManipulator _objects;

        public TextNodeSealer(INodeManipulator nodeValues,
            IStringPrimitiveScanner scanner, IDynamicFacade facade,
            ISerializerSettings settings) : base(facade, nodeValues, settings)
        {
            _values = nodeValues;
            _typeProvider = nodeValues;
            _facade = facade;

            _typeManipulator = facade.TypeManipulator;
            _scanner = scanner;
            _objects = facade.ObjectManipulator;
        }

        public override void CloseNode(ITextNode node)
        {
            _values.TryBuildValue(node);

            if (node.NodeType == NodeTypes.None)
                node.NodeType = _typeProvider.GetNodeType(node, Settings.SerializationDepth);

            switch (node.NodeType)
            {
                case NodeTypes.Object:

                    #region object = sum of attributes (primitives) and children (should already be imbued)

                    foreach (var attr in node.Attributes)
                    {
                        if (attr.Key == DasCoreSerializer.RefTag ||
                            attr.Key == DasCoreSerializer.RefAttr)
                        {
                            BuildFromReference(node, attr.Value);
                            return;
                        }

                        var type = _typeManipulator.GetPropertyType(node.Type, attr.Key);

                        if (type == null)
                            continue;

                        var str = attr.Value;

                        var val = _scanner.GetValue(str, type) ?? str;
                        var wal = node.Value;
                        _objects.SetProperty(node.Type, attr.Key, ref wal, val);
                        node.Value = wal;
                    }

                    break;

                #endregion

                case NodeTypes.Collection:
                    ConstructCollection(ref node);
                    break;

                case NodeTypes.Primitive:

                    #region build primitive

                    var nodeText = node.Text.ToString();
                    if (!String.IsNullOrWhiteSpace(nodeText))
                        node.Value = _scanner.GetValue(nodeText, node.Type);
                    break;

                #endregion

                case NodeTypes.PropertiesToConstructor:
                    ConstructFromProperties(ref node);
                    break;
                case NodeTypes.Dynamic:

                    #region collate property types, generate type, set prop values

                    if (_facade.TypeInferrer.IsCollection(node.Type))
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
                        var atVal = _scanner.GetValue(attr.Value, typeof(Object));
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
                        {
                            if (value is String str)
                            {
                                var propType = dynamicType.GetPropertyType(propName);
                                value = _scanner.GetValue(str, propType);
                            }
                        }

                        var wal = node.Value;
                        dynamicType.SetPropertyValue(ref wal, propName, value);
                        node.Value = wal;
                    }

                    break;

                #endregion

                case NodeTypes.Fallback:

                    #region serialize/maybe takes string as a constructor

                    if (node.Text == null)
                        break;
                    var txt = node.Text.ToString();

                    if (txt.Length == 0)
                    {
                        node.Value = null;
                        return;
                    }

                    var isGoodReturnType = !_facade.TypeInferrer.IsUseless(node.Type);

                    if (!isGoodReturnType)
                    {
                        node.Value = _scanner.GetValue(txt, node.Type);
                    }
                    else
                    {
                        node.Value = _scanner.GetValue(txt, node.Type);
                        Imbue(node.Parent, node.Name, node.Value);
                    }

                    break;

                #endregion
            }

            var parent = node.Parent;

            if (parent != null)
                Imbue(parent, node.Name, node.Value);
        }

        private void BuildFromReference(ITextNode node, String refValue)
        {
            var refLength = refValue.Length;
            var chain = new Stack<ITextNode>();
            var current = node.Parent;
            while (current != null)
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
            if (node.Parent != null)
                Imbue(node.Parent, node.Name, node.Value);
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

            if (node.Children.TryGetValue(propKey, out var child))
            {
                val = child.Value;
                return true;
            }

            if (node.DynamicProperties.TryGetValue(propKey, out val))
                return true;

            return false;
        }
    }
}