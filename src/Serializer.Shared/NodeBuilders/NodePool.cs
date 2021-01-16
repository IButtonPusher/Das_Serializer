using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Das.Serializer.Objects;
using Das.Serializer.Printers;

namespace Das.Serializer
{
    public class NodePool : INodePool
    {
        public NodePool(ISerializerSettings settings,
                        INodeTypeProvider nodeTypeProvider)
        {
            _nodeTypeProvider = nodeTypeProvider;
            Settings = settings;
        }

        public IPrintNode GetPrintNode(INamedValue namedValue)
        {
            return GetPrintNodeImpl(namedValue);
        }

        public IPrintNode GetPrintNode(INamedValue namedValue,
                                       Object? overrideValue)
        {
            var node = GetPrintNodeImpl(namedValue);
            node.SetValue(overrideValue);
            return node;
        }

        public INamedValue GetNamedValue(String name,
                                         Object value,
                                         Type type)
        {
            var buffer = NamedNodeBuffer.Value!;

            if (buffer.Count == 0)
                return new NamedValueNode(ReturnToSender, name, value, type);

            var print = buffer.Dequeue();
            print.Set(name, value, type);
            return print;
        }

        public INamedValue GetNamedValue(DictionaryEntry kvp)
        {
            return GetNamedValue(kvp.Key.ToString()!, kvp.Value!,
                kvp.Value?.GetType() ?? typeof(Object));
        }

        public IProperty GetProperty(String propertyName,
                                     Object? propertyValue,
                                     Type propertyType,
                                     Type declaringType)
        {
            var buffer = PropertyBuffer.Value!;

            if (buffer.Count > 0)
            {
                var print = buffer.Dequeue();
                print.Set(propertyName, propertyValue, propertyType);
                return print;
            }

            return new PropertyValueNode(ReturnToSender, propertyName, propertyValue,
                propertyType);
        }

        public ISerializerSettings Settings { get; }

        [MethodImpl(256)]
        private PrintNode GetPrintNodeImpl(INamedValue namedValue)
        {
            var buffer = PrintNodeBuffer.Value!;
            var nodeType = _nodeTypeProvider.GetNodeType(namedValue.Type, Settings.SerializationDepth);

            if (buffer.Count <= 0)
                return new PrintNode(ReturnToSender, namedValue, nodeType);

            var print = buffer.Dequeue();
            print.Set(namedValue, nodeType);
            return print;
        }

        private static void ReturnToSender(PrintNode node)
        {
            var buffer = PrintNodeBuffer.Value!;
            buffer.Enqueue(node);
        }

        private static void ReturnToSender(NamedValueNode node)
        {
            var buffer = NamedNodeBuffer.Value!;
            buffer.Enqueue(node);
        }

        private static void ReturnToSender(PropertyValueNode node)
        {
            var buffer = PropertyBuffer.Value!;
            buffer.Enqueue(node);
        }

        private static readonly ThreadLocal<Queue<PrintNode>> PrintNodeBuffer =
            new ThreadLocal<Queue<PrintNode>>(
                () => new Queue<PrintNode>());

        private static readonly ThreadLocal<Queue<NamedValueNode>> NamedNodeBuffer =
            new ThreadLocal<Queue<NamedValueNode>>(
                () => new Queue<NamedValueNode>());

        private static readonly ThreadLocal<Queue<PropertyValueNode>> PropertyBuffer =
            new ThreadLocal<Queue<PropertyValueNode>>(
                () => new Queue<PropertyValueNode>());

        private readonly INodeTypeProvider _nodeTypeProvider;
    }
}
