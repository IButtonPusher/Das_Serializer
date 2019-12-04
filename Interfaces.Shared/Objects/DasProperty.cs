using System;

namespace Das.Serializer
{
    public class DasProperty : DasMember
    {

        public DasAttribute[] Attributes { get; set; }

        public DasProperty(String name, Type type) : base(name, type)
        {
        }
    }
}