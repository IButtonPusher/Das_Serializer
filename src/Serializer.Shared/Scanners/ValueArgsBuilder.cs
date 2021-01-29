using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public readonly struct ValueArgsBuilder : IValueSetter

    {
        private readonly Dictionary<String, Int32> _nameMapping;

        public Object?[] Values { get; }

        public ValueArgsBuilder(ParameterInfo[] ctorParams)
        {
            Values = new Object?[ctorParams.Length];
            _nameMapping = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < ctorParams.Length; c++)
                _nameMapping.Add(ctorParams[c].Name, c);
        }

        public bool SetValue(String propName,
                             ref Object targetObj,
                             Object? propVal,
                             SerializationDepth depth)
        {
            if (!_nameMapping.TryGetValue(propName, out var index))
                return false;

            Values[index] = propVal;
            return true;
        }

        public object? GetValue(Object o,
                                String propertyName)
        {
            return default;
        }
    }
}
