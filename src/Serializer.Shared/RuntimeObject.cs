using System;
using System.Collections.Generic;
using System.Text;

namespace Das.Serializer
{
    public class RuntimeObject<T> : RuntimeObject
    {
        public static implicit operator T(RuntimeObject<T> obj)
        {
            switch (obj)
            {
                case T good:
                    return good;
            }

            throw new InvalidCastException();
        }
    }

    public class RuntimeObject
    {
        public RuntimeObject()
        {
            Properties = new Dictionary<String, RuntimeObject>();
        }

        public RuntimeObject? this[String key]
        {
            get
            {
                if (Properties.TryGetValue(key, out var res))
                    return res;

                return default;
            }
        }

        public Dictionary<String, RuntimeObject> Properties { get; }

        public Object? PrimitiveValue { get; set; }
    }
}
