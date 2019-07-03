using System;

namespace Das.Serializer.Objects
{
    /// <summary>
    /// A type/value association
    /// </summary>
    public class ValueNode
    {
        public ValueNode(object value) : this(value, value?.GetType()) { }

        public ValueNode(object value, Type type)
        {
            Value = value;
            Type = type;
        }

        public Object Value { get; }

        public Type Type { get; set; }

        public override string ToString() => (Type?.Name ?? "?") + ": = " + Value;
    }
}
