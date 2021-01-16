using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface IPropertyValueIterator<out TProp> : IProperty, IEnumerable<TProp>
        where TProp : class, INamedValue
    {
        Int32 Count { get; }

        TProp this[Int32 index] { get; }

        void Clear();

        Boolean MoveNext();
    }
}