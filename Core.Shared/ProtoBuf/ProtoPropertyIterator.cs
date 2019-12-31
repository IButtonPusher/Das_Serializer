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
            _structStack = new Stack<IProtoStructure>();
            _propertyStack = new Stack<Int32>();
            _valueStack = new Stack<Object>();
        }

        public void Set(Object value)
        {
            Parent = null;
            _object = value;
            _current = 0;
            _count = _protoStruct.GetValueCount(value);
        }

        private Object _object;
        private Type _type;
        //private ProtoWireTypes _wireType;
        //private Int32 _header;
        private Int32 _count;
        private Int32 _fieldIndex;
        private IProtoFieldAccessor _currentField;
        private readonly Stack<IProtoStructure> _structStack;
        private readonly Stack<Int32> _propertyStack;
        private readonly Stack<Object> _valueStack;
        public IProtoPropertyIterator Parent { get; set; }


        private IProtoStructure _protoStruct;
        //private String _name;
        protected Int32 _current;

        public Boolean MoveNext()
        {
            if (_current >= _count )
                return false;

            _currentField = _protoStruct[_current];
            _fieldIndex = _currentField.Index;
            Value = _currentField.GetValue(_object);
            _type = _currentField.Type;
            
            _current++;
            return true;
        }

        public IProtoPropertyIterator Push()
        {
            var child = _protoStruct.GetPropertyValues(Value, _current);
            child.Parent = this;
            return child;

            _propertyStack.Push(_current);
            _structStack.Push(_protoStruct);
            _valueStack.Push(_object);
            _protoStruct = _protoStruct.PropertyStructures[_currentField.Index];
            _current = 0;
            _object = Value;
            _count = _protoStruct.GetValueCount(_object);
            
        }

        public IProtoPropertyIterator Pop()
        {
            return Parent;

            // if (_propertyStack.Count == 0)
            //     return false;
            // _current = _propertyStack.Pop();
            // _protoStruct = _structStack.Pop();
            //
            // _object = _valueStack.Pop();
            // _count = _protoStruct.GetValueCount(_object);
            // return true;
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
            private set => _count = value;
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
