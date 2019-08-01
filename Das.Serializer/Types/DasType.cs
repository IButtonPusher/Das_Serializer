using System;
using System.Collections.Generic;
using System.Linq;

namespace Das.Serializer
{
    public class DasType : IDynamicType
    {
        private readonly Dictionary<String, DasProperty> _properties;

        public DasType(Type managedType, IEnumerable<DasProperty> properties)
        {
            _properties = properties.ToDictionary(p => p.Name, p => p);
            ManagedType = managedType;
            PublicSetters = new Dictionary<String, PropertySetter>();
            PublicGetters = new Dictionary<String, Func<object, object>>();
        }

        public Dictionary<String, PropertySetter> PublicSetters { get; }
        public Dictionary<String, Func<Object, Object>> PublicGetters { get; }
        public Type ManagedType { get; }

        public bool IsLegalValue(string forProperty, object value)
            => _properties.TryGetValue(forProperty, out var prop) &&
               prop.Type.IsInstanceOfType(value);

        public Type GetPropertyType(string propertyName)
            => _properties.TryGetValue(propertyName, out var prop) ? prop.Type : default;

        public static implicit operator Type(DasType das)
            => das.ManagedType;

        public bool SetPropertyValue(ref object targetObj, string propName,
            object propVal)
        {
            if (!PublicSetters.TryGetValue(propName, out var setter))
                return false;

            setter(ref targetObj, propVal);
            return true;
        }

        public bool TryGetPropertyValue(object obj, string propertyName,
            out object result)
        {
            if (!(PublicGetters.TryGetValue(propertyName, out var getter)))
            {
                result = default;
                return false;
            }

            result = getter(obj);
            return true;
        }
    }
}