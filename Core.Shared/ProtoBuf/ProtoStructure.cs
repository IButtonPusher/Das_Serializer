﻿using Das.Serializer.Objects;
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
        protected ProtoStructure(Type type, ISerializationDepth depth,
            ITypeManipulator state,  INodePool nodePool, IInstantiator instantiator)
            : base(type, true, depth, state, nodePool)
        {
            FieldMap = new Dictionary<Int32, IProtoFieldAccessor>();
            _headers = new Dictionary<String, Int32>();
            _defaultConstructor = instantiator.GetDefaultConstructor(type);
            PropertyStructures = new Dictionary<Int32, IProtoStructure>();

            _propertyIterators = new ThreadLocal<ProtoPropertyIterator>(() =>
                new ProtoPropertyIterator(this));
        }

        public IProtoFieldAccessor this[Int32 idx] => _memberTypes[idx];
        public Dictionary<Int32, IProtoStructure> PropertyStructures { get; }

        public virtual Int32 GetValueCount(Object _) => _propGetterList.Count; 

        public KeyValuePair<String, Func<Object, Object>> GetGetter(Int32 index)
            => _propGetterList[index];  

        protected readonly Dictionary<String, Int32> _headers;
        protected IProtoFieldAccessor[] _memberTypes;
        private readonly Func<Object> _defaultConstructor;
        

        public Boolean TryGetHeader(INamedField field, out Int32 header) 
            => _headers.TryGetValue(field.Name, out header);


        public Dictionary<Int32, IProtoFieldAccessor> FieldMap { get; }

        public virtual Boolean IsRepeating(ref ProtoWireTypes wireType,
            ref TypeCode typeCode, ref Type type) => false;

        public void Set(IProtoFeeder byteFeeder, Int32 fieldHeader)
        {
            
        }

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

        public virtual IProtoPropertyIterator GetPropertyValues(Object o)
        {
            var itar = _propertyIterators.Value;
            itar.Set(o);
            return itar;
        }

        public IProtoPropertyIterator GetPropertyValues(Object o, Int32 fieldIndex)
        {
            return PropertyStructures[fieldIndex].GetPropertyValues(o);
        }

        public Object BuildDefault() => _defaultConstructor();

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
            ITypeManipulator state, ProtoBufOptions<TPropertyAttribute> options,INodePool nodePool,
            ISerializationCore serializerCore) 
            : base(type, depth, state,nodePool, serializerCore.ObjectInstantiator)
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

                var isValidLengthDelim = wire == ProtoWireTypes.LengthDelimited
                                         && prop.Type != Const.StrType && prop.Type != Const.ByteArrayType;

                if (isValidLengthDelim)
                {
                    var propStructure = state.GetPrintProtoStructure(prop.Type, 
                        options, serializerCore);

                    PropertyStructures[index] = propStructure;
                }

                
                
                _headers[name] = header;

                var getter = GetGetter(i);

                var tc = Type.GetTypeCode(prop.Type);

                var isCollection = isValidLengthDelim && state.IsCollection(prop.Type);

                var protoField = new ProtoField(prop.Name, prop.Type, wire, 
                    index, header, getter.Value, tc,
                    IsLeaf(prop.Type, true), isCollection);

                _memberTypes[i++] = protoField;
                FieldMap[index] = protoField;
            }
        }

    }
}
