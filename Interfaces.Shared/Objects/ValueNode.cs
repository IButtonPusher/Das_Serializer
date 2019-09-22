using System;

namespace Das.Serializer.Objects
{
    /// <summary>
    /// A type/value association
    /// </summary>
    public class ValueNode
    {
        public ValueNode(Object value) : this(value, value?.GetType())
        {
        }

        public ValueNode(Object value, Type type)
        {
            Value = value;
            Type = type;
        }

        public Object Value { get; }

        public Type Type { get; set; }

        public override String ToString() => (Type?.Name ?? "?") + ": = " + Value;
    }
}