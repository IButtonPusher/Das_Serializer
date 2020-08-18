using System;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    public class PropertyValueNode : NamedValueNode, IProperty
    {
        public PropertyValueNode(Action<PropertyValueNode> returnToSender, String propertyName,
            Object propertyValue, Type propertyType, Type declaringType)
        {
            _isEmptyInitialized = -1;
            _value = propertyValue;
            _name = propertyName;
            _type = propertyType;
            _returnToSender = returnToSender;
            DeclaringType = declaringType;
        }

        protected PropertyValueNode()
        {
        }

        public Type DeclaringType { get; private set; }

        public override void Dispose()
        {
            _returnToSender(this);
        }

        public void Set(String propertyName, Object propertyValue,
            Type propertyType, Type declaringType)
        {
            DeclaringType = declaringType;
            _name = propertyName;
            _isEmptyInitialized = -1;
            _type = propertyType;
            _value = propertyValue;
        }

        private readonly Action<PropertyValueNode> _returnToSender;
    }
}