using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPrintNode : INamedValue
    {
        Boolean IsWrapping { get; set; }

        NodeTypes NodeType { get; set; }
    }
}
