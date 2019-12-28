using Das.Serializer.Objects;
using Das.Serializer.ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Das.Serializer
{
    public abstract class ProtoStructure : TypeStructure, IProtoStructure
    {
        public ProtoStructure(Type type, ISerializationDepth depth,
            ITypeManipulator state,  INodePool nodePool)
            : base(type, true, depth, state, nodePool)
        {
            FieldMap = new Dictionary<Int32, IProtoFieldAccessor>();
            _headers = new Dictionary<String, Int32>();
            WireTypes = new Dictionary<String, ProtoWireTypes>();
            PropertyStructures = new Dictionary<Int32, IProtoStructure>();

            _propertyIterators = new ThreadLocal<ProtoPropertyIterator>(() =>
                new ProtoPropertyIterator(this));
        }

        public IProtoFieldAccessor this[Int32 idx] => _memberTypes[idx];
        public Dictionary<Int32, IProtoStructure> PropertyStructures { get; }

        public Int32 GetterCount => _propGetterList.Count; 

        public KeyValuePair<String, Func<Object, Object>> GetGetter(Int32 index)
            => _propGetterList[index];  

        protected readonly Dictionary<String, Int32> _headers;
        protected IProtoFieldAccessor[] _memberTypes;
        

        public Boolean TryGetHeader(INamedField field, out Int32 header) 
            => _headers.TryGetValue(field.Name, out header);


        public Dictionary<Int32, IProtoFieldAccessor> FieldMap { get; }

        public Dictionary<String, ProtoWireTypes> WireTypes { get; }

        Boolean IProtoStructure.IsCollection => false;

        private static readonly ConcurrentDictionary<Type, ProtoWireTypes> _wireTypes;

        protected readonly ThreadLocal<ProtoPropertyIterator> _propertyIterators;

        public static ProtoWireTypes GetWireType(Type type)
        {
            if (!_wireTypes.TryGetValue(type, out var wire))
            {
                if (_enumType.IsAssignableFrom(type))
                    wire = 0;
                else wire = ProtoWireTypes.LengthDelimited;

                _wireTypes[type] = wire;
            }

            return wire;
        }

        public abstract IProtoPropertyIterator GetPropertyValues(object o);

        private static readonly Type _enumType = typeof(Enum);
        static ProtoStructure()
        {
            var wireTypes = new Dictionary<Type, ProtoWireTypes>
            {
                {typeof(Int32), ProtoWireTypes.Varint},
                {typeof(Int64), ProtoWireTypes.Varint},
                {typeof(UInt32), ProtoWireTypes.Varint},
                {typeof(UInt64), ProtoWireTypes.Varint},
                {typeof(Byte), ProtoWireTypes.Varint},
                {typeof(Boolean), ProtoWireTypes.Varint},
                {typeof(Enum), ProtoWireTypes.Varint},
                {typeof(Double), ProtoWireTypes.Int64},
                {typeof(String), ProtoWireTypes.LengthDelimited},
                {typeof(Byte[]), ProtoWireTypes.LengthDelimited},
                {typeof(Single), ProtoWireTypes.Int32}
            };

            _wireTypes = new ConcurrentDictionary<Type, ProtoWireTypes>(wireTypes);
        }
    }

    public class ProtoStructure<TPropertyAttribute> : ProtoStructure
        where TPropertyAttribute : Attribute
    {
        public ProtoStructure(Type type, ISerializationDepth depth, 
            ITypeManipulator state, ProtoBufOptions<TPropertyAttribute> options,INodePool nodePool) 
            : base(type, depth, state,nodePool)
        {
            var propsMaybe = GetMembersToSerialize(depth).ToArray();
            
            var i = 0;
            _memberTypes = new IProtoFieldAccessor[propsMaybe.Length];

            foreach (var prop in propsMaybe)
            {
                var name = prop.Name;

                if (!TryGetAttribute<TPropertyAttribute>(name, out var attributes))
                    continue;

                var index = options.GetIndex(attributes);
                var wire = GetWireType(prop.Type);
                var header = (Int32)wire + (index << 3);

                if (wire == ProtoWireTypes.LengthDelimited
                    && prop.Type != Const.StrType)
                {
                    var propStructure = new ProtoStructure<TPropertyAttribute>(
                        prop.Type, depth, state, options, nodePool);
                    PropertyStructures[index] = propStructure;
                }

                WireTypes[name] = wire;
                
                _headers[name] = header;

                var getter = GetGetter(i);

                var tc = Type.GetTypeCode(prop.Type);

                var protoField = new ProtoField(prop, wire, index, header, getter.Value, tc,
                    IsLeaf(prop.Type, true));

                _memberTypes[i++] = protoField;
                FieldMap[index] = protoField;
            }
        }


        public override IProtoPropertyIterator GetPropertyValues(Object o)
        {
            var itar = _propertyIterators.Value;
            itar.Set(o);
            return itar;
        }

      
    }
}
