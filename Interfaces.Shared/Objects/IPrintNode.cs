using System;

namespace Das.Serializer
{
    public interface IPrintNode :  INamedValue
    {
        NodeTypes NodeType { get; set; }
        Boolean IsWrapping { get; set; }
    }
}
