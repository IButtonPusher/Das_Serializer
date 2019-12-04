using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface IProtoStructure : ITypeStructure
    {
        Dictionary<Int32, INamedField> FieldMap { get; }
    }
}
