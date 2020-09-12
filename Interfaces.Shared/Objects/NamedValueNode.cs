using System;
using System.Collections;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    /// <summary>
    ///     A named type/value association
    /// </summary>
    public class NamedValueNode : ValueNode, INamedValue
    {
        public NamedValueNode(Action<NamedValueNode> returnToSender,
                              String name,
                              Object value,
                              Type type)
            : this(name, value, type)
        {
            _returnToSender = returnToSender;
        }

#pragma warning disable 8618
        protected NamedValueNode()
#pragma warning restore 8618
        {
        }

        protected NamedValueNode(String name, Object value, Type type) : base(value, type)
        {
            Set(name, value, type);
        }

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

        public virtual void Dispose()
        {
            _returnToSender(this);
        }

        public String Name => _name;

        public virtual void Set(String name, Object value, Type type)
        {
            _name = name;
            _isEmptyInitialized = -1;
            _type = type;
            _value = value;
        }

        public override String ToString()
        {
            return "[" + Name + "]  " + base.ToString();
        }

        private readonly Action<NamedValueNode> _returnToSender;
        protected Int32 _isEmptyInitialized;
        protected String _name;
    }
}