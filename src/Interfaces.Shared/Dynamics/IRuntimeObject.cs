using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IRuntimeObject
    {
        IRuntimeObject? this[String key] { get; }

        Object? PrimitiveValue { get; set; }

        Dictionary<String, IRuntimeObject> Properties { get; }

        Type GetObjectType();
    }
}
