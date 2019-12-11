using System;
using Das.Serializer;
using Das.Serializer.Objects;

namespace Serializer.Core.Printers
{
    public class PrintNode : NamedValueNode, IPrintNode
    {
        private readonly Action<PrintNode> _returnToSender;

        public PrintNode(Action<PrintNode> returnToSender,
            String name, Object value, Type type, NodeTypes nodeType,
            Boolean isWrapping = false)
            : base(name, value, type)
        {
            _returnToSender = returnToSender;
            NodeType = nodeType;
            IsWrapping = isWrapping;
        }

        public PrintNode(Action<PrintNode> returnToSender,
            INamedValue valu, NodeTypes nodeType)
            : this(returnToSender, valu.Name, valu.Value, valu.Type, nodeType)
        {
        }

        public void Set(INamedValue valu, NodeTypes nodeType)
        {
            NodeType = nodeType;
            IsWrapping = false;
            Name = valu.Name;
            Value = valu.Value;
            Type = valu.Type;
            _isEmptyInitialized = -1;
        }

        public NodeTypes NodeType { get; set; }

        public Boolean IsWrapping { get; set; }

        public override String ToString() => NodeType + "\\" + 
                                             IsWrapping + "/ " + base.ToString();

        public override void Dispose()
        {
            _returnToSender(this);
        }
    }
}