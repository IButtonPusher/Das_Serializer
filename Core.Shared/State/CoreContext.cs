using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using Das.Serializer;

namespace Serializer.Core
{
    public abstract class CoreContext : SerializerCore, ISerializationContext
    {
        public CoreContext(ISerializationCore dynamicFacade, ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            _typeConverters = new ConcurrentDictionary<Type, TypeConverter>();
        }

        public abstract IScanNodeProvider ScanNodeProvider { get; }

        private readonly ConcurrentDictionary<Type, TypeConverter> _typeConverters;

        public TypeConverter GetTypeConverter(Type type)
            => _typeConverters.TryGetValue(type, out var found) ? found :
                _typeConverters.GetOrAdd(type, TypeDescriptor.GetConverter(type));
        
    }
}