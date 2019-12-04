using System;

namespace Das.Serializer
{
    public class DasMember : INamedField
    {
        public DasMember(String name, Type type)
        {
            Name = name;
            Type = type;
        }

        public String Name { get; }
        public Type Type { get; }
    }
}
