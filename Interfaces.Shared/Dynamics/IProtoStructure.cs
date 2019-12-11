using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface IProtoStructure : ITypeStructure
    {
        Dictionary<Int32, INamedField> FieldMap { get; }

        /// <summary>
        /// returns wire type as first 3 bits then field index as an int per proto spec
        /// </summary>
        Boolean TryGetHeader(INamedField field, out Int32 header);
    }
}
