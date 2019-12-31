using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Objects;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryPrinter : ProtoDictionaryStructure,
        IProtoStructure, IProtoPropertyIterator, IProtoFieldAccessor
    {
        public ProtoDictionaryPrinter(Type type, ISerializationDepth depth, ITypeManipulator state,
            INodePool nodePool, ISerializationCore serializerCore)
            : base(type, depth, state, nodePool, serializerCore)
        {
            _subIndex = 0;
            _dictionaryType = type;
        }

        private Int32 _subIndex;
        private IDictionaryEnumerator _enumerator;
        private Type _dictionaryType;
        private Type _type;
        private ProtoWireTypes _wireType;
        private TypeCode _typeCode;
        private Boolean _isRepeated;
        private Int32 _iteratedCount;
        private Int32 _index;
        protected Object _currentValue;

        //private ProtoField _current;

        public IProtoPropertyIterator Parent { get; set; }

        public Int32 Index => _index;

        public TypeCode TypeCode { get; protected set; }
        Boolean IProtoField.IsLeafType => throw new NotSupportedException(); //_current.IsLeafType;

       


        public override IProtoPropertyIterator GetPropertyValues(Object o)
        {
            GetValueCount(o);
            return this;
        }

        IProtoFieldAccessor IProtoStructure.this[Int32 index]
        {
            get
            {
                _index = index;
                return this;
            }
        }

        public override Int32 GetValueCount(Object obj)
        {
            _iteratedCount = 0;
            var dict = (IDictionary) obj;
            _enumerator = dict.GetEnumerator();
            _value = dict;
            TypeCode = TypeCode.Object;
            return _value.Count;
        }

        /// <summary>
        /// Sub index:
        /// 0 => print the header for the property that this dictionary holds the value to
        /// 1 => print the key's header as field index 1 + its wire type
        /// 2 => print the key's serialized bytes
        /// 3 => print the value's header as field index 2 + its wire type
        /// 4 => print the value's serialized bytes
        /// -1 => yield so that the full kvp's length can be printed and the stack can reset
        /// -2 => all kvps have been printed and the next call to Pop should take us back to the parent
        /// </summary>
        /// <returns></returns>
        public Boolean MoveNext()
        {
            switch (_subIndex)
            {
                case 0:
                    if (!_enumerator.MoveNext())
                    {
                        _subIndex = -2;
                        return false;
                    }

                    _subIndex = 1;
                    _currentValue = _enumerator.Current ?? throw new InvalidOperationException();
                    _type = typeof(DictionaryEntry);
                    _wireType = ProtoWireTypes.LengthDelimited;
                    _typeCode = TypeCode.Object;
                    _isRepeated = false;

                    // _current = new ProtoField(string.Empty, _type, _wireType, 0, 0, default,
                    //     _typeCode, false, _isRepeated);

                    return true;
                case 1:
                    _wireType = KeyWireType;
                    // _wireType = GetWireType(_enumerator.Key?.GetType() ?? 
                    //                         throw new InvalidOperationException());
                    _currentValue = (Int32)_wireType + (1 << 3);
                    _type = Const.IntType;
                    _typeCode = TypeCode.Int32;
                    _wireType = ProtoWireTypes.Varint;
                    _subIndex = 2;
                    _isRepeated = true;

                    // _current = new ProtoField(string.Empty, _type, _wireType, 0, 0, default,
                    //     _typeCode, false, _isRepeated);

                    return true;
                case 2:
                    _currentValue = _enumerator.Key?? throw new InvalidOperationException();
                    _type = KeyType;
                    _typeCode = KeyTypeCode;
                    _wireType = KeyWireType;
                    _subIndex = 3;
                    _isRepeated = true;
                    
                    // _current = new ProtoField(string.Empty, _type, _wireType, 0, 0, default,
                    //     _typeCode, false, _isRepeated);

                    break;
                case 3:
                    //_wireType = GetWireType(_enumerator.Value.GetType());
                    _wireType = ValueWireType;
                    _currentValue = (Int32)_wireType + (2 << 3);
                    _type = Const.IntType;
                    _typeCode = TypeCode.Int32;
                    _subIndex = 4;
                    _wireType = ProtoWireTypes.Varint;
                    _isRepeated = true;
                    
                    // _current = new ProtoField(string.Empty, _type, _wireType, 0, 0, default,
                    //     _typeCode, false, _isRepeated);

                    return true;
                case 4:
                    _currentValue = _enumerator.Value;
                    _type = ValueType;
                    _typeCode = ValueTypeCode;
                    _wireType = ValueWireType;
                    _subIndex = -1;
                    break;
                case -1:
                    //finished with this kvp.  Return false so we write the length and reset 
                    //for a new instance
                    _subIndex = 0;
                    _iteratedCount++;
                    return false;
            }

            // _type = ValueType;
            // _typeCode = ValueTypeCode;
            // _wireType = ValueWireType;
            
            // var t = _currentValue.GetType();
            // if (t != _type)
            // { }
            //
            // _type = t;
            // //_isRepeated = _subIndex != 1 || _state.IsCollection(_type);
            //
            // var tc = Type.GetTypeCode(_type);
            // if (tc != _typeCode)
            // { }
            //
            // _typeCode = tc;
            // var wt = ProtoStructure.GetWireType(_type);
            // if (wt != _wireType)
            // { }
            //
            // _wireType = wt;
            return true;
        }

        IProtoProperty IPropertyValueIterator<IProtoProperty>.this[Int32 index] => throw new NotImplementedException();

        void IPropertyValueIterator<IProtoProperty>.Clear()
        {
            throw new NotImplementedException();
        }

        Int32 IPropertyValueIterator<IProtoProperty>.Count => throw new NotImplementedException();

        IProtoPropertyIterator IProtoPropertyIterator.Push()
        {
            switch (_subIndex)
            {
                case 1:
                    return this;
                default:
                    throw new NotImplementedException();
            }
        }

        IProtoPropertyIterator IProtoPropertyIterator.Pop()
        {
            switch (_subIndex)
            {
                case -2:
                    return Parent;
                default:
                    if (_iteratedCount < _value.Count)
                        return this;
                    else return Parent;
            }
        }

        Type IStronglyTyped.Type
        {
            get => _type;
            set => _type = value;
        }

        Type ITypeStructureBase.Type => _dictionaryType;

        ProtoWireTypes IProtoField.WireType => _wireType;

        TypeCode IProtoField.TypeCode => _typeCode;

        Boolean IProtoField.IsRepeated => _isRepeated;
        public Object GetValue(Object @from)
        {
            if (!_enumerator.MoveNext())
                return null;
            return _enumerator.Current;
        }

        Int32 IProtoField.Header => Parent.Header;
        String INamedField.Name => throw new NotSupportedException(); //_current.Name;

        Object IValueNode.Value => _currentValue; 

        void IDisposable.Dispose()
        {
            
        }

        Boolean INamedValue.IsEmptyInitialized => throw new NotImplementedException();

        Type IProperty.DeclaringType => throw new NotImplementedException();

        IEnumerator<IProtoProperty> IEnumerable<IProtoProperty>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
