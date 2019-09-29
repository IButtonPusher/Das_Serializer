using System;

namespace Das
{
    public class SerializeAsTypeAttribute : Attribute
    {
        public Type TargetType { get; }

        public SerializeAsTypeAttribute(Type type)
        {
            TargetType = type;
        }
    }
}