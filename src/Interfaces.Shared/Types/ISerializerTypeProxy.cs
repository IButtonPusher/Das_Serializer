using System;
using System.Collections.Generic;

namespace Das.Serializer.Types
{
    public interface ISerializerTypeProxy<in TType, TMany, TFew, in TWriter>
        where TMany : IEnumerable<TFew>
        where TWriter : IRemunerable<TMany, TFew>
    {
        void Print(TType obj,
                   TWriter writer);
    }

    public interface ISerializerTypeProxy<in TType>
    {
        void Print<TWriter>(TType obj,
                            TWriter writer)
            where TWriter : ITextRemunerable;
    }
}
