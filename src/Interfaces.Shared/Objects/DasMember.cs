using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class DasMember : INamedField
    {
        public DasMember(String name,
                         Type type)
        {
            name = String.Intern(name);
            Name = name;
            _hash = name.GetHashCode() + (type.GetHashCode() ^ 3);
            Name = name;
            Type = type;
        }

        public String Name { get; }

        public Type Type { get; }

        public Boolean Equals(INamedField other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return other.Type == Type && other.Name == Name;
        }


        public override Int32 GetHashCode()
        {
            return _hash;
        }

        public override String ToString()
        {
            return $"{Type.Name} {Name}";
        }

        private readonly Int32 _hash;
    }
}
