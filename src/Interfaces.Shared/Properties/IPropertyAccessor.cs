using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IPropertyAccessor
    {
        Boolean CanRead { get; }

        Boolean CanWrite { get; }

        Type DeclaringType { get; }

        Type PropertyType { get; }

        /// <summary>
        /// The property's name under most circumstances.  Can also be ParentType.PropertyName etc
        /// </summary>
        String PropertyPath { get; }

        Object? GetPropertyValue(Object obj);

        Boolean SetPropertyValue(ref Object targetObj,
                                 Object? propVal);

        //Boolean SetPropertyValue<TTarget>(ref TTarget targetObj,
        //                                  Object? propVal);

        Boolean TryGetPropertyValue(Object obj,
                                    out Object result);
    }
}
