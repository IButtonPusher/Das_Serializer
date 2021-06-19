using System;
using System.Collections.Concurrent;

namespace Das.Serializer.Types
{
    public static class PropertyDictionary<TObject, TProperty>
    {
        public static ConcurrentDictionary<String, IPropertyAccessor<TObject, TProperty>> Properties { get; }
            = new ();
    }
}
