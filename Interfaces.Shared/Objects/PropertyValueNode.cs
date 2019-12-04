using System;

namespace Das.Serializer.Objects
{
    public class PropertyValueNode : NamedValueNode
    {
        public Type DeclaringType { get; }

        public PropertyValueNode(String propertyName, Object propertyValue, Type propertyType, Type declaringType)
            : base(propertyName, propertyValue, propertyType)
        {
            DeclaringType = declaringType;
        }
    }
}
