using System;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface IProtoStructure : ITypeStructure
    {
        IProtoFieldAccessor this[Int32 idx] {get;}

        Int32 GetterCount { get; }

        Dictionary<Int32, IProtoStructure> PropertyStructures { get; }

        Dictionary<Int32, IProtoFieldAccessor> FieldMap { get; }

        Boolean IsCollection { get; }

        /// <summary>
        /// returns wire type as first 3 bits then field index as an int per proto spec
        /// </summary>
        Boolean TryGetHeader(INamedField field, out Int32 header);

        IProtoPropertyIterator GetPropertyValues(Object o);
    }
}
