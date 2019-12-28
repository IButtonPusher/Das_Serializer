using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoCollectionPrinter : ProtoStructure, IProtoStructure
        , IProtoPropertyIterator
    {
        public ProtoCollectionPrinter(Type type, ISerializationDepth depth, 
            ITypeManipulator state, INodePool nodePool)
            : base(type, depth, state, nodePool)
        {
            _germaneType = GetGermaneType(type);
            _index = -1;
            WireType = GetWireType(_germaneType);
        }

        public ProtoWireTypes WireType { get; }
        public Int32 Header { get; }
        public Int32 Index => _index;

        public Boolean IsLeafType { get; }

        public TypeCode TypeCode { get; }

        IProtoProperty IPropertyValueIterator<IProtoProperty>.
            this[Int32 index] => throw new NotImplementedException();

        IProtoFieldAccessor IProtoStructure.this[Int32 index] =>  throw new NotImplementedException();

        Type IStronglyTyped.Type
        {
            get => _germaneType;
            set => throw new NotSupportedException();
        }

        private readonly Type _germaneType;
        private Object _currentValue;
        private Int32 _index;
        private IList _values;

        public override IProtoPropertyIterator GetPropertyValues(Object o)
        {
            if (o is IList ok)
                _values = ok;
            else
            {
                _values = new List<Object>();
                var collection = (IEnumerable) o;

                foreach (var item in collection)
                {
                    _values.Add(item);
                }
            }

            _index = -1;

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

        public Boolean MoveNext()
        {
            if (_index >= _values.Count - 1)
                return false;

            _index++;
            _currentValue = _values[_index];
            return true;
        }

        public void Clear()
        {
            _index = -1;
        }

        public void Push()
        {
            throw new NotImplementedException();
        }

        public Boolean Pop()
        {
            throw new NotImplementedException();
        }

        Boolean IProtoStructure.IsCollection => true;

        Boolean IProtoPropertyIterator.IsCollection => true;

        public Int32 Count => _values.Count;
    }
}
