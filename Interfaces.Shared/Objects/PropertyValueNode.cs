using System;

namespace Das.Serializer.Objects
{
    public class PropertyValueNode : NamedValueNode, IProperty
    {
        private readonly Action<PropertyValueNode> _returnToSender;

        public Type DeclaringType
        {
            get => _declaringType;
        }

        private Type _declaringType;

        public PropertyValueNode(Action<PropertyValueNode> returnToSender, String propertyName, 
            Object propertyValue, Type propertyType, Type declaringType)
        {
            _value = propertyValue;
            _name = propertyName;
            _type= propertyType;
            _returnToSender = returnToSender;
            _declaringType = declaringType;
        }

        protected PropertyValueNode(){}

        public void Set(String propertyName, Object propertyValue,
            Type propertyType, Type declaringType)
        {
            _declaringType = declaringType;
            _name= propertyName;
            _isEmptyInitialized = -1;
            _type= propertyType;
            _value = propertyValue;
        }

        public override void Dispose()
        {
            _returnToSender(this);
        }
    }
}
