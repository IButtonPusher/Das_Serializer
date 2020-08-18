using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class DasProperty : DasMember
    {
        public DasProperty(String name, Type type, DasAttribute[] attributes) 
            : base(name, type)
        {
            Attributes = attributes;
        }

        public DasAttribute[] Attributes { get; }
    }
}