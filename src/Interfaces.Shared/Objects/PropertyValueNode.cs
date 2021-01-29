using System;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    public class PropertyValueNode : NamedValueNode,
                                     IProperty
    {
        public PropertyValueNode(Action<PropertyValueNode> returnToSender,
                                 String propertyName,
                                 Object? propertyValue,
                                 Type propertyType)
        {
            _isEmptyInitialized = -1;
            _value = propertyValue;
            _name = propertyName;
            _type = propertyType;
            _returnToSender = returnToSender;
        }

#pragma warning disable 8618
        // ReSharper disable once UnusedMember.Global
        protected PropertyValueNode()
#pragma warning restore 8618
        {
        }

        //public Type DeclaringType { get; private set; }

        public override void Dispose()
        {
            _returnToSender(this);
        }

        public override void Set(String propertyName,
                                 Object? propertyValue,
                                 Type? propertyType)
        {
            _name = propertyName;
            _isEmptyInitialized = -1;
            _type = propertyType;
            _value = propertyValue;
        }

        private readonly Action<PropertyValueNode> _returnToSender;
    }
}
