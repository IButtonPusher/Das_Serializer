using System;
using Das.Serializer;
using Das.Serializer.Objects;

namespace Serializer.Core.Printers
{
    public class PrintNode : NamedValueNode
    {
        public PrintNode(String name, Object value, Type type, NodeTypes nodeType,
            Boolean isWrapping = false)
            : base(name, value, type)
        {
            NodeType = nodeType;
            IsWrapping = isWrapping;
        }

        public PrintNode(NamedValueNode valu, NodeTypes nodeType)
            : this(valu.Name, valu.Value, valu.Type, nodeType)
        {
        }

        public NodeTypes NodeType { get; set; }

        public Boolean IsWrapping { get; }

        public override String ToString()
            => NodeType.ToString() + "\\" + IsWrapping + "/ " + base.ToString();
    }
}