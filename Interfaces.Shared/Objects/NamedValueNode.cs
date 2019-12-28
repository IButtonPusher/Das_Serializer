using System;
using System.Collections;

namespace Das.Serializer.Objects
{
    /// <summary>
    /// A named type/value association
    /// </summary>
    public class NamedValueNode : ValueNode, INamedValue
    {
        private readonly Action<NamedValueNode> _returnToSender;
        protected Int32 _isEmptyInitialized;
        protected String _name;

        public Boolean IsEmptyInitialized
        {
            get
            {
                switch (_isEmptyInitialized)
                {
                    case -1:
                        _isEmptyInitialized = Value is ICollection countable
                            ? countable.Count
                            : 1;
                        goto default;
                    default:
                        return _isEmptyInitialized == 0;
                }
            }
        }
        
        public NamedValueNode(Action<NamedValueNode> returnToSender, 
            String name, Object value, Type type) 
            : this(name, value, type)
        {
            _returnToSender = returnToSender;
        }

        protected NamedValueNode(){}

        protected NamedValueNode(String name, Object value, Type type) : base(value, type)
        {
            Set(name, value, type);
        }

        public void Set(String name, Object value, Type type)
        {
            _name = name;
            _isEmptyInitialized = -1;
            _type = type;
            _value = value;
        }

        public Boolean Equals(INamedField other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return other.Type == Type && other.Name == Name;
        }

        public override String ToString() => "[" + Name + "]  " + base.ToString();
        public virtual void Dispose()
        {
            _returnToSender(this);
        }

        public String Name => _name;

    }
}