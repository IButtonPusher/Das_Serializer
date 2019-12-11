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
            Value = value;
            Type = type;
        }

        protected void Set(Object value, Type type)
        {
            Value = value;
            Type = type;
        }

        public Object Value { get; set; }

        public Type Type { get; set; }

        public override String ToString() => (Type?.Name ?? "?") + ": = " + Value;
    }
}