using System;
using System.Collections.Generic;

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

        public RuntimeObject(Object? primitiveValue)
            : this()
        {
            PrimitiveValue = primitiveValue;
        }

        public Type GetObjectType() => PrimitiveValue != null
            ? PrimitiveValue.GetType()
            : Const.ObjectType;

        public RuntimeObject? this[String key]
        {
            get
            {
                if (Properties.TryGetValue(key, out var res))
                    return res;

                return default;
            }
        }

        public override string ToString()
        {
            if (PrimitiveValue is { } p)
                return p.ToString();

            return GetType().Name + " - " + Properties.Count + " properties";
        }

        public Dictionary<String, RuntimeObject> Properties { get; }

        public Object? PrimitiveValue { get; set; }
    }
}
