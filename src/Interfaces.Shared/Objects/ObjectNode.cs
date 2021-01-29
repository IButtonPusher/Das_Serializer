using System;
using System.Threading.Tasks;
using Das.Serializer.Objects;

namespace Das.Printers
{
    public class ObjectNode : NamedValueNode
    {
        public ObjectNode(Object value,
                          Type type,
                          Int32 index) :
            base("V" + index, value, type)
        {
            Index = index;
        }

        public Int32 Index { get; }
    }
}
