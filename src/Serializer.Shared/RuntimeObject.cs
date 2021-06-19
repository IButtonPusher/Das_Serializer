using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    //public class RuntimeObject<T> : RuntimeObject
    //{
    //    public static implicit operator T(RuntimeObject<T> obj)
    //    {
    //        switch (obj)
    //        {
    //            case T good:
    //                return good;
    //        }

    //        throw new InvalidCastException();
    //    }
    //}

    public class RuntimeObject : IRuntimeObject
    {
        public RuntimeObject(ITypeManipulator typeManipulator)
        {
            _typeManipulator = typeManipulator;
            _hash = base.GetHashCode();
            Properties = new Dictionary<String, IRuntimeObject>();
        }

        public RuntimeObject(ITypeManipulator typeManipulator,
                             Object? primitiveValue)
            : this(typeManipulator)
        {
            PrimitiveValue = primitiveValue;
            if (primitiveValue is { } notNull)
                _hash = notNull.GetHashCode();
        }

        public IRuntimeObject? this[String key]
        {
            get
            {
                if (Properties.TryGetValue(key, out var res))
                    return res;

                return default;
            }
        }

        public Object? PrimitiveValue { get; set; }

        public Dictionary<String, IRuntimeObject> Properties { get; }

        public Type GetObjectType()
        {
            return PrimitiveValue != null
                ? PrimitiveValue.GetType()
                : Const.ObjectType;
        }

        public override bool Equals(Object? obj)

        {
            switch (obj)
            {
                case null:
                    return false;

                case RuntimeObject robj:
                    if (robj.Properties.Count != Properties.Count)
                        return false;

                    foreach (var kvp in robj.Properties)
                    {
                        if (!Properties.TryGetValue(kvp.Key, out var myValue))
                            return false;

                        if (!Equals(kvp.Value, myValue))
                            return false;
                    }

                    return true;

                default:
                    if (PrimitiveValue != null)
                        return Equals(PrimitiveValue, obj);

                    var ts = _typeManipulator.GetTypeStructure(obj.GetType());//, DepthConstants.AllProperties);

                    if (ts.PropertyCount != Properties.Count)
                        return false;

                    foreach (var kvp in ts.IteratePropertyValues(obj, DepthConstants.AllProperties))
                    {
                        if (!Properties.TryGetValue(kvp.Key.Name, out var myValue))
                            return false;

                        if (!Equals(kvp.Value, myValue))
                            return false;
                    }

                    return true;
            }
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override String ToString()
        {
            if (PrimitiveValue is { } p)
                return p.ToString();

            return GetType().Name + " - " + Properties.Count + " properties";
        }

        private readonly Int32 _hash;
        private readonly ITypeManipulator _typeManipulator;
    }
}
