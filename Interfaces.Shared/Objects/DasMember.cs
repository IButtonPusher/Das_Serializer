using System;

namespace Das.Serializer
{
    public class DasMember : INamedField
    {
        public DasMember(String name, Type type)
        {
            name = String.Intern(name);
            Name = name;
            _hash = name.GetHashCode() + (type.GetHashCode() ^ 3);
            Name = name;
            Type = type;
        }

        private readonly Int32 _hash;

        public String Name { get; }
        public Type Type { get; set; }
        public Boolean Equals(INamedField other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return other.Type == Type && other.Name == Name;
        }

        public override String ToString() => $"{Type.Name} {Name}";
        

        public override Int32 GetHashCode() => _hash;

    }
}
