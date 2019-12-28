using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Das.Serializer.Objects;
using Serializer.Core.Printers;

namespace Das.Serializer
{
    public class NodePool : INodePool
    {
        private readonly INodeTypeProvider _nodeTypeProvider;

        public NodePool(ISerializerSettings settings, INodeTypeProvider nodeTypeProvider)
        {
            _nodeTypeProvider = nodeTypeProvider;
            Settings = settings;
        }
        
        public ISerializerSettings Settings { get; }

        private static readonly ThreadLocal<Queue<PrintNode>> PrintNodeBuffer = 
            new ThreadLocal<Queue<PrintNode>>(
            () => new Queue<PrintNode>());

        private static readonly ThreadLocal<Queue<NamedValueNode>> NamedNodeBuffer =
            new ThreadLocal<Queue<NamedValueNode>>(
                () => new Queue<NamedValueNode>());

        private static readonly ThreadLocal<Queue<PropertyValueNode>> PropertyBuffer =
            new ThreadLocal<Queue<PropertyValueNode>>(
                () => new Queue<PropertyValueNode>());

        public IPrintNode GetPrintNode(INamedValue namedValue) 
            => GetPrintNodeImpl(namedValue);

        [MethodImpl(256)]
        private PrintNode GetPrintNodeImpl(INamedValue namedValue)
        {
            var buffer = PrintNodeBuffer.Value;
            var nodeType = _nodeTypeProvider.GetNodeType(namedValue.Type, Settings.SerializationDepth);

            if (buffer.Count > 0)
            {
                var print = buffer.Dequeue();
                print.Set(namedValue, nodeType);
                return print;
            }

            return new PrintNode(ReturnToSender, namedValue, nodeType);
        }

        private static void ReturnToSender(PrintNode node)
        {
            var buffer = PrintNodeBuffer.Value;
            buffer.Enqueue(node);
        }

        private static void ReturnToSender(NamedValueNode node)
        {
            var buffer = NamedNodeBuffer.Value;
            buffer.Enqueue(node);
        }

        private static void ReturnToSender(PropertyValueNode node)
        {
            var buffer = PropertyBuffer.Value;
            buffer.Enqueue(node);
        }

        public IPrintNode GetPrintNode(INamedValue namedValue, Object overrideValue)
        {
            var node = GetPrintNodeImpl(namedValue);
            node.SetValue(overrideValue);
            return node;
        }

        public INamedValue GetNamedValue(String name, Object value, Type type)
        {
            var buffer = NamedNodeBuffer.Value;

            if (buffer.Count > 0)
            {
                var print = buffer.Dequeue();
                print.Set(name, value, type);
                return print;
            }

            return new NamedValueNode(ReturnToSender, name, value, type);
        }

        public INamedValue GetNamedValue(DictionaryEntry kvp)
            => GetNamedValue(kvp.Key.ToString(), kvp.Value,
                kvp.Value?.GetType() ?? typeof(Object));

        public IProperty GetProperty(String propertyName, Object propertyValue, Type propertyType,
            Type declaringType)
        {
            var buffer = PropertyBuffer.Value;

            if (buffer.Count > 0)
            {
                var print = buffer.Dequeue();
                print.Set(propertyName, propertyValue, propertyType, declaringType);
                return print;
            }

            return new PropertyValueNode(ReturnToSender, propertyName, propertyValue,
                propertyType, declaringType);

        }
    }
}
