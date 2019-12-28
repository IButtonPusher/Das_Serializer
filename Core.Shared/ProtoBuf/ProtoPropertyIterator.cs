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
            //_value = o;
            _protoStruct = protoStruct;
            _structStack = new Stack<IProtoStructure>();
            _propertyStack = new Stack<Int32>();
            _valueStack = new Stack<Object>();
            Count = protoStruct.GetterCount;
        }

        public void Set(Object value)
        {
            _value = value;
            _current = 0;
        }

        private Object _value;
        private Type _type;
        private ProtoWireTypes _wireType;
        private Int32 _header;
        private IProtoFieldAccessor _currentField;
        private Stack<IProtoStructure> _structStack;
        private Stack<Int32> _propertyStack;
        private Stack<Object> _valueStack;
        
        
        private IProtoStructure _protoStruct;
        private String _name;
        protected Int32 _current;

        public Boolean MoveNext()
        {
            if (_current >= Count )
                return false;

            _currentField = _protoStruct[_current];
            
            Value = _currentField.GetValue(_value);
            _name = _currentField.Name;
            _type = _currentField.Type;
            _wireType = _currentField.WireType;
            _header = _currentField.Header;
            _current++;
            return true;
        }

        public void Push()
        {
            _propertyStack.Push(_current);
            _structStack.Push(_protoStruct);
            _valueStack.Push(_value);
            _protoStruct = _protoStruct.PropertyStructures[_currentField.Index];
            _current = 0;
            Count = _protoStruct.GetterCount;
            _value = Value;
        }

        public Boolean Pop()
        {
            if (_propertyStack.Count == 0)
                return false;
            _current = _propertyStack.Pop();
            _protoStruct = _structStack.Pop();
            Count = _protoStruct.GetterCount;
            _value = _valueStack.Pop();
            return true;
        }

        public Boolean IsCollection => _protoStruct.IsCollection;

        public ProtoWireTypes WireType => _wireType;
        public Int32 Header => _header;
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

        public Int32 Count { get; private set; }

        public Type Type
        {
            get => _type; 
            set => _type = value;
        }
        public String Name => _name;
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
