using System;
using System.Collections.Generic;

namespace Das.Serializer
{
    public interface IProtoStructureBase : ITypeStructureBase
    {
        Object BuildDefault();

        Dictionary<Int32, IProtoFieldAccessor> FieldMap { get; }
    }
}
