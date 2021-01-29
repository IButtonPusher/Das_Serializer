using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public abstract class CoreContext : SerializerCore,
                                        ISerializationContext
    {
        public CoreContext(ISerializationCore dynamicFacade,
                           ISerializerSettings settings)
            : base(dynamicFacade, settings)
        {
            //_typeConverters = new ConcurrentDictionary<Type, TypeConverter>();
        }

        public abstract IScanNodeProvider ScanNodeProvider { get; }

        //public TypeConverter GetTypeConverter(Type type)
        //{
        //    return _typeConverters.TryGetValue(type, out var found)
        //        ? found
        //        : _typeConverters.GetOrAdd(type, TypeDescriptor.GetConverter(type));
        //}

        //private readonly ConcurrentDictionary<Type, TypeConverter> _typeConverters;
    }
}
