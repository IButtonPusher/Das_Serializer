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

        public abstract INodeProvider NodeProvider { get; }

        private readonly ConcurrentDictionary<Type, TypeConverter> _typeConverters;

        public NodeTypes GetNodeType(INode node, SerializationDepth depth)
            => NodeProvider.TypeProvider.GetNodeType(node, depth);

        public NodeTypes GetNodeType(Type type, SerializationDepth depth)
            => NodeProvider.TypeProvider.GetNodeType(type, depth);

        public TypeConverter GetTypeConverter(Type type)
            => _typeConverters.TryGetValue(type, out var found) ? found :
                _typeConverters.GetOrAdd(type, TypeDescriptor.GetConverter(type));
        
    }
}