using Das.Serializer.ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Das.Serializer
{
    public class ProtoStructure<TPropertyAttribute> : TypeStructure, IProtoStructure
        where TPropertyAttribute : Attribute
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ConcurrentDictionary<Type, ProtoWireTypes> _wireTypes;
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Type _enumType = typeof(Enum);

        static ProtoStructure()
        {
            var wireTypes = new Dictionary<Type, ProtoWireTypes>
            {
                {typeof(Int32), ProtoWireTypes.Varint},
                {typeof(Int64), ProtoWireTypes.Varint},
                {typeof(UInt32), ProtoWireTypes.Varint},
                {typeof(UInt64), ProtoWireTypes.Varint},
                {typeof(Boolean), ProtoWireTypes.Varint},
                {typeof(Enum), ProtoWireTypes.Varint},
                {typeof(Double), ProtoWireTypes.Int64},
                {typeof(String), ProtoWireTypes.LengthDelimited},
                {typeof(Byte[]), ProtoWireTypes.LengthDelimited},
                {typeof(Single), ProtoWireTypes.Int32}
            };

            _wireTypes = new ConcurrentDictionary<Type, ProtoWireTypes>(wireTypes);
        }

        public ProtoStructure(Type type, ISerializationDepth depth, 
            ITypeManipulator state, ProtoBufOptions<TPropertyAttribute> options,INodePool nodePool) 
            : base(type, true, depth, state,nodePool)
        {
            FieldMap = new Dictionary<Int32, INamedField>();
            _headers = new Dictionary<INamedField, Int32>();
            var propsMaybe = GetMembersToSerialize(depth);

            foreach (var prop in propsMaybe)
            {
                if (!TryGetAttribute<TPropertyAttribute>(prop.Name, out var attributes))
                    continue;

                var index = options.GetIndex(attributes);
                var wire = GetWireType(prop);
                var header = (Int32)wire + (index << 3);
                
                FieldMap[index] = prop;
                
                
                _headers[prop] = header;
            }
        }

        private static ProtoWireTypes GetWireType(INamedField node)
        {
            if (!_wireTypes.TryGetValue(node.Type, out var wire))
            {
                if (_enumType.IsAssignableFrom(node.Type))
                    wire = 0;
                else wire = ProtoWireTypes.LengthDelimited;

                _wireTypes[node.Type] = wire;
            }

            return wire;
        }

        public Dictionary<Int32, INamedField> FieldMap { get; }

        private readonly Dictionary<INamedField, Int32> _headers;
        public Boolean TryGetHeader(INamedField field, out Int32 header) 
            => _headers.TryGetValue(field, out header);

    }
}
