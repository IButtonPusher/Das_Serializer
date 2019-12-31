using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoCollectionPrinter : ProtoStructure, IProtoStructure
        , IProtoPropertyIterator, IProtoFieldAccessor
    {
        public ProtoCollectionPrinter(Type type, ISerializationDepth depth, 
            ITypeManipulator state, INodePool nodePool, IInstantiator instantiator)
            : base(type, depth, state, nodePool,instantiator)
        {
            _germaneType = GetGermaneType(type);
            _index = -1;
            WireType = GetWireType(_germaneType);
        }

        public IProtoPropertyIterator Parent { get; set; }

        public override Int32 GetValueCount(Object obj)
        {
            switch (obj)
            {
                case IDictionary dict:
                    _enumerator = dict.GetEnumerator();
                    _value = dict;
                    _germaneType = typeof(DictionaryEntry);
                    TypeCode = TypeCode.Object;
                    break;
                case ICollection coll:
                    _enumerator = coll.GetEnumerator();
                    _value = coll;
                    TypeCode = Type.GetTypeCode(_germaneType);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return _value.Count;
        }

        public ProtoWireTypes WireType { get; }
        public Int32 Header { get; }
        public Int32 Index => _index;

        private Int32 _stackHeight;

        public Boolean IsLeafType { get; }
        public Boolean IsRepeated => throw new NotImplementedException();

        public Object GetValue(Object @from)
        {
            if (!_enumerator.MoveNext())
                return null;
            return _enumerator.Current;
        }

        public TypeCode TypeCode { get; protected set; }

        IProtoProperty IPropertyValueIterator<IProtoProperty>.
            this[Int32 index] => throw new NotImplementedException();

        IProtoFieldAccessor IProtoStructure.this[Int32 index]
        {
            get
            {
                _index = index;
                return this;
            }
        }

        Type IStronglyTyped.Type
        {
            get => _germaneType;
            set => throw new NotSupportedException();
        }

        protected Type _germaneType;
        private ICollection _value;
        protected Object _currentValue;
        private Int32 _index;
        private IList _values;
        private IEnumerator _enumerator;

        public override IProtoPropertyIterator GetPropertyValues(Object o)
        {
            GetValueCount(o);
            return this;
        }

        public String Name { get; }
        public Object Value => _currentValue;
        public void Dispose()
        {
            
        }

        public Boolean IsEmptyInitialized { get; }
        public Type DeclaringType => Type;
        public IEnumerator<IProtoProperty> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual Boolean MoveNext()
        {
            if (!_enumerator.MoveNext())
                return false;

            _currentValue = _enumerator.Current;
            return true;
        }

        public void Clear()
        {
            _index = -1;
        }

        public IProtoPropertyIterator Push()
        {
            _stackHeight++;
            return this;
        }

        public IProtoPropertyIterator Pop()
        {

            if (_stackHeight-- > 0)
                return this;
            return Parent;
        }

        // Boolean IProtoScanStructure.IsRepeating(ref ProtoWireTypes wireType,ref TypeCode typeCodes,
        //     ref Type type) 
        //     => true;

        public Int32 Count => _values.Count;
    }
}
