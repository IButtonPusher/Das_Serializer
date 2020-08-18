using System;
using System.Threading.Tasks;
using Das.Serializer.Objects;

namespace Das.Serializer.Printers
{
    public class PrintNode : NamedValueNode, IPrintNode
    {
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

        public NodeTypes NodeType { get; set; }

        public Boolean IsWrapping { get; set; }

        public override void Dispose()
        {
            _returnToSender(this);
        }

        public void Set(INamedValue valu, NodeTypes nodeType)
        {
            NodeType = nodeType;
            IsWrapping = false;
            _name = valu.Name;
            _value = valu.Value;
            _type = valu.Type;
            _isEmptyInitialized = -1;
        }

        public void SetValue(Object? value)
        {
            _value = value;
        }

        public override String ToString()
        {
            return NodeType + "\\" +
                   IsWrapping + "/ " + base.ToString();
        }

        private readonly Action<PrintNode> _returnToSender;
    }
}