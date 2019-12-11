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
        private Int32 _hash;

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
            String name, Object value, Type type) : this(name, value, type)
        {
            _returnToSender = returnToSender;
        }

        protected NamedValueNode(String name, Object value, Type type) : base(value, type)
        {
            Set(name, value, type);
        }

        public void Set(String name, Object value, Type type)
        {
            name = String.Intern(name);
            Name = name;
            _hash = name.GetHashCode() + (type.GetHashCode() ^ 3);
            _isEmptyInitialized = -1;
            base.Set(value, type);
        }

        public Boolean Equals(INamedField other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return other.Type == Type && other.Name == Name;
        }

        public override Int32 GetHashCode() => _hash;

        public override String ToString() => "[" + Name + "]  " + base.ToString();
        public virtual void Dispose()
        {
            _returnToSender(this);
        }

        public String Name { get; set; }

//        public static implicit operator NamedValueNode(DictionaryEntry kvp) =>
//            new NamedValueNode(kvp.Key.ToString(), kvp.Value,
//                kvp.Value?.GetType() ?? typeof(Object));
    }
}