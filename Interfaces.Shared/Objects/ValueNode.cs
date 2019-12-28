using System;

namespace Das.Serializer.Objects
{
    /// <summary>
    /// A type/value association
    /// </summary>
    public class ValueNode : IValueNode
    {
        public ValueNode(Object value) : this(value, value?.GetType())
        {
        }

        protected ValueNode(){}

        public ValueNode(Object value, Type type)
        {
            _value = value;
            _type= type;
        }

        protected void Set(Object value, Type type)
        {
            _value = value;
            _type= type;
        }

        public Object Value => _value;

        public Type Type
        {
            get => _type;
            set => _type = value;
        }

        public override String ToString() => (Type?.Name ?? "?") + ": = " + Value;

        protected Object _value;
        protected Type _type;
    }
}