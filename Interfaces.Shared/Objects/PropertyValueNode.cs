using System;

namespace Das.Serializer.Objects
{
    public class PropertyValueNode : NamedValueNode, IProperty
    {
        private readonly Action<PropertyValueNode> _returnToSender;
        public Type DeclaringType { get; set; }

        public PropertyValueNode(Action<PropertyValueNode> returnToSender, String propertyName, 
            Object propertyValue, Type propertyType, Type declaringType)
            : base(propertyName, propertyValue, propertyType)
        {
            _returnToSender = returnToSender;
            DeclaringType = declaringType;
        }

        public void Set(String propertyName, Object propertyValue,
            Type propertyType, Type declaringType)
        {
            DeclaringType = declaringType;
            base.Set(propertyName, propertyValue, propertyType);
        }

        public override void Dispose()
        {
            _returnToSender(this);
        }
    }
}
