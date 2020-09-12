using System;
using System.Threading.Tasks;

namespace Das
{
    public class SerializeAsTypeAttribute : Attribute
    {
        public SerializeAsTypeAttribute(Type type)
        {
            TargetType = type;
        }

        public Type TargetType { get; }
    }
}