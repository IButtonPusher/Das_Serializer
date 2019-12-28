using System;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface IPropertyValueIterator<out TProp> : IProperty, IEnumerable<TProp>
    where TProp : class, INamedValue
    {
        Boolean MoveNext();

        TProp this[Int32 index] { get; }

        void Clear();

        Int32 Count { get; }
    }
}
