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
                              IStringPrimitiveScanner scanner,
                              ISerializationCore facade,
                              ISerializerSettings settings,
                              String refPathIndicator)
            : base(facade, nodeValues, settings)
        {
            _values = nodeValues;
            _typeProvider = facade.NodeTypeProvider;
            _facade = facade;
            _refPathIndicator = refPathIndicator;

            _typeManipulator = facade.TypeManipulator;
            _scanner = scanner;
            _objects = facade.ObjectManipulator;
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

                    // object = sum of attributes (primitives) and children (should already be imbued)

                    _values.TryBuildValue(node);

                    foreach (var attr in node.Attributes)
                    {
                        var key = attr.Key;
                        var str = attr.Value.Value;

                        if (key == Const.RefTag ||
                            key == Const.RefAttr)
                        {
                            BuildFromReference(node, str);
                            return;
                        }

                        var type = node.Type != null
                            ? _typeManipulator.GetPropertyType(node.Type, key)
                            : null;

                        if (type == null && node.Type != null &&
                            key.Length > 0 && char.IsLower(key[0]))
                        {
                            key = _facade.TypeInferrer.ToPropertyStyle(key);
                            type = _typeManipulator.GetPropertyType(node.Type, key);
                        }

                        if (type == null || node.Type == null)
                            continue;


                        //trying to force it into a string leads to setting object's values to strings
                        //which goes poorly...
                        //var val = _scanner.GetValue(str, type) ?? str;

                        var val = _scanner.GetValue(str, type, !attr.Value.WasValueInQuotes);

                        if (val == null)
                        {
                            if (Settings.PropertySearchDepth == TextPropertySearchDepths.AsTypeInLoadedModules)
                                val = str;
                            else if (Settings.AttributeValueSurrogates.TryGetValue(node,
                                key, str, out var found))
                                val = found;
                            else continue;
                        }

                        var wal = node.Value;
                        _objects.SetProperty(node.Type, key, ref wal!, val);
                    }

                    break;

                case NodeTypes.Collection:
                    ConstructCollection(ref node);
                    break;

                case NodeTypes.Primitive:
                    var nodeText = node.Text;
                    if (node.Type != null && !String.IsNullOrWhiteSpace(nodeText))
                        node.Value = _scanner.GetValue(nodeText, node.Type, false);
                    break;

                case NodeTypes.PropertiesToConstructor:
                    ConstructFromProperties(ref node);
                    break;
                case NodeTypes.Dynamic:


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
                        var atVal = _scanner.GetValue(attr.Value.Value, Const.ObjectType,
                            attr.Value.WasValueInQuotes);
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

                                value = _scanner.GetValue(str, propType, false);
                            }

                        var wal = node.Value;
                        dynamicType.SetPropertyValue(ref wal!, propName, value);
                        node.Value = wal;
                    }

                    break;


                case NodeTypes.Fallback:

                    // serialize/maybe takes string as a constructor

                    if (node.Text.Length == 0)
                    {
                        node.Value = null;
                        return;
                    }

                    var txt = node.Text;

                    var isGoodReturnType = !_facade.TypeInferrer.IsUseless(node.Type);

                    if (node.Type != null)
                        node.Value = _scanner.GetValue(txt, node.Type, false);

                    if (isGoodReturnType && node.Value != null)
                        Imbue(node.Parent, node.Name, node.Value);

                    break;
            }

            var parent = node.Parent;

            if (NullNode != parent && node.Value != null)
                Imbue(parent, node.Name, node.Value);
        }


        public override Boolean TryGetPropertyValue(ITextNode node,
                                                    String key,
                                                    Type propertyType,
                                                    out Object? val)
        {
            var propKey = _facade.TypeInferrer.ToPropertyStyle(key);

            if (node.TryGetAttribute(propKey, false, out var attrib))
            {
                val = _scanner.GetValue(attrib.Value, propertyType, attrib.WasValueInQuotes);
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

        private void BuildFromReference(ITextNode node,
                                        String refValue)
        {
            var refLength = refValue.Length;
            var chain = new Stack<ITextNode>();
            var current = node.Parent;

            //var root = current.Value;

            while (NullNode != current)
            {
              //  root = current.Value;

                chain.Push(current);
                current = current.Parent;
            }

            current = chain.Pop();

            var sb = new StringBuilder(_refPathIndicator +  current.Type?.FullName ?? current.Name);
            
            while (sb.Length < refLength)
            {
                current = chain.Pop();
                sb.Append($"/{current.Type?.FullName ?? current.Name}");
                
                //sb.Append($"/{current.Name}");
            }

            if (current.Value == null)
                _values.TryBuildValue(current);

            node.Value = current.Value;
            if (NullNode != node.Parent && node.Value != null)
                Imbue(node.Parent, node.Name, node.Value);
        }

        private static readonly NullNode NullNode = NullNode.Instance;
        private readonly ISerializationCore _facade;
        private readonly String _refPathIndicator;
        private readonly IObjectManipulator _objects;

        private readonly IStringPrimitiveScanner _scanner;

        //private readonly IAttributeValueSurrogates _attributeValueSurrogates;
        private readonly ITypeManipulator _typeManipulator;
        private readonly INodeTypeProvider _typeProvider;
        private readonly INodeManipulator _values;
    }
}
