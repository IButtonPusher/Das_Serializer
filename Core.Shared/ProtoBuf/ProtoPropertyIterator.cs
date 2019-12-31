using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoPropertyIterator : IProtoPropertyIterator
    {
        public ProtoPropertyIterator(IProtoStructure protoStruct)
        {
            _protoStruct = protoStruct;
        }

        public void Set(Object value)
        {
            Parent = null;
            _object = value;
            _current = 0;
            _count = _protoStruct.GetValueCount(value);
        }

        private Object _object;
        private Int32 _count;
        private IProtoFieldAccessor _currentField;
        public IProtoPropertyIterator Parent { get; set; }


        private IProtoStructure _protoStruct;
        //private String _name;
        protected Int32 _current;

        public Boolean MoveNext()
        {
            if (_current >= _count )
                return false;

            _currentField = _protoStruct[_current];
            Value = _currentField.GetValue(_object);

            _current++;
            return true;
        }

        public IProtoPropertyIterator Push()
        {
            var child = _protoStruct.GetPropertyValues(Value, _current);
            child.Parent = this;
            return child;
        }

        public IProtoPropertyIterator Pop()
        {
            return Parent;
        }

        public Boolean IsRepeated => _currentField.IsRepeated;

        public ProtoWireTypes WireType => _currentField.WireType;
        public Int32 Header => _currentField.Header;
        public Int32 Index => _currentField.Index;

        public TypeCode TypeCode => _currentField.TypeCode;
        public Boolean IsLeafType => _currentField.IsLeafType;
       

        public  IEnumerator<IProtoProperty> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IProtoProperty IPropertyValueIterator<IProtoProperty>.this[Int32 index] 
            => throw new NotImplementedException();

        public void Clear()
        {
            
        }

        public Int32 Count
        {
            get => _count;
            //private set => _count = value;
        }

        public Type Type
        {
            get =>  _currentField.Type; 
            set =>  throw new NotSupportedException();//_type = value;
        }
        public String Name => _currentField.Name;
        public Object Value { get; private set; }
        public void Dispose()
        {
            
        }

        public Boolean IsEmptyInitialized { get; }
        public Type DeclaringType => _protoStruct.Type;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
