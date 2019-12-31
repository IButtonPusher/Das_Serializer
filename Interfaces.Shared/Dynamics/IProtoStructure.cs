using System;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface IProtoStructure : ITypeStructure, IProtoScanStructure
    {
        IProtoFieldAccessor this[Int32 idx] {get;}

        Int32 GetValueCount(Object obj);

        Dictionary<Int32, IProtoStructure> PropertyStructures { get; }                

        /// <summary>
        /// returns wire type as first 3 bits then field index as an int per proto spec
        /// </summary>
        Boolean TryGetHeader(INamedField field, out Int32 header);

        IProtoPropertyIterator GetPropertyValues(Object o);

        IProtoPropertyIterator GetPropertyValues(Object o, Int32 fieldIndex);

        
    }
}
