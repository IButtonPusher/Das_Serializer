using System;
using System.Collections;

namespace Das.Serializer.Objects
{
    /// <summary>
    /// A named type/value association
    /// </summary>
    public class NamedValueNode : ValueNode, INamedField
    {
        public NamedValueNode(String name, Object value, Type type) : base(value, type)
        {
            Name = name;
        }

        public override String ToString() => "[" + Name + "]  " + base.ToString();

        public String Name { get; }

        public static implicit operator NamedValueNode(DictionaryEntry kvp) =>
            new NamedValueNode(kvp.Key.ToString(), kvp.Value,
                kvp.Value?.GetType() ?? typeof(Object));
    }
}