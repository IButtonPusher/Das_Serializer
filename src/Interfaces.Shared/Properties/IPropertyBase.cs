using System;
using System.Reflection;

namespace Das.Serializer
{
    public interface IPropertyBase : IPropertyInfo
    {
        Boolean CanRead { get; }

        Boolean CanWrite { get; }

        Type DeclaringType { get; }

        PropertyInfo PropertyInfo {get;}

        /// <summary>
        ///     The property's name under most circumstances.  Can also be ParentType.PropertyName etc
        /// </summary>
        String PropertyPath { get; }

        Type PropertyType { get; }


        Boolean TryGetAttribute<TAttribute>(out TAttribute value)
            where TAttribute : Attribute;
    }
}
