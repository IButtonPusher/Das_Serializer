using System;

namespace Das.Serializer
{
    public class DasProperty
    {
        public String Name { get; set; }
        public Type Type { get; set; }

        public DasAttribute[] Attributes { get; set; }

        public DasProperty(String name, Type type)
        {
            Name = name;
            Type = type;
        }
    }
}