using System;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    /// <summary>
    ///     A type/value association
    /// </summary>
    public class ValueNode : IValueNode
    {
        public ValueNode(Object value) : this(value, value?.GetType())
        {
        }

        protected ValueNode()
        {
        }

        public ValueNode(Object value, Type? type)
        {
            _value = value;
            _type = type;
        }

        public Object? Value => _value;

        public Type? Type
        {
            get => _type;
            set => _type = value;
        }

        // ReSharper disable once UnusedMember.Global
        protected void Set(Object value, Type type)
        {
            _value = value;
            _type = type;
        }

        public override String ToString()
        {
            return (Type?.Name ?? "?") + ": = " + Value;
        }

        protected Type? _type;

        protected Object? _value;
    }
}