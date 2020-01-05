using System;
using System.Collections;
using System.Collections.Generic;
using Das.Serializer.Objects;
using Das.Serializer.Remunerators;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryPrinter : ProtoDictionaryStructure,
        IProtoStructure, IProtoPropertyIterator, IProtoFieldAccessor
    {
        public ProtoDictionaryPrinter(Type type, ISerializationDepth depth, ITypeManipulator state,
            INodePool nodePool, ISerializationCore serializerCore, 
            IProtoWriter binaryWriter)
            : base(type, depth, state, nodePool, serializerCore)
        {
            _subIndex = 0;
            _dictionaryType = type;
            _binaryWriter = binaryWriter;
        }

        private Int32 _subIndex;
        private IDictionaryEnumerator _enumerator;
        private Type _dictionaryType;
        private readonly IProtoWriter _binaryWriter;
        private Type _type;
        private ProtoWireTypes _wireType;
        private TypeCode _typeCode;
        private Boolean _isRepeated;
        private Int32 _iteratedCount;
        private Int32 _index;
        protected Object _currentValue;
        private IProtoPropertyIterator _parent;
        private Int32 _header;

        //private ProtoField _current;

        public IProtoPropertyIterator Parent
        {
            get => _parent;
            set
            {
                _parent = value;
                _header = _parent.Header;
            }
        }

        public Int32 Index => _index;

        Boolean IProtoField.IsLeafType => throw new NotSupportedException(); //_current.IsLeafType;

       


        public override IProtoPropertyIterator GetPropertyValues(Object o)
        {
            //GetValueCount(o);
            _iteratedCount = 0;
            var dict = (IDictionary) o;
            _enumerator = dict.GetEnumerator();
            _value = dict;
            _typeCode = TypeCode.Object;
            return this;
        }

        public Boolean MoveNext()
        {
            throw new NotImplementedException();
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
            _typeCode = TypeCode.Object;
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
        public Boolean MoveNext(ref IProtoPropertyIterator propertyValues)
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

                    _binaryWriter.WriteInt32(_parent.Header);

                    propertyValues = propertyValues.Push();

                    _binaryWriter.Push();

                    goto one;
                case 1:
                    one:
                   
                    var v = (Int32)KeyWireType + (1 << 3);
                    _binaryWriter.WriteInt32(v);
                    goto two;
                case 2:
                    two:
                    _currentValue = _enumerator.Key;
                    _isRepeated = true;

                    if (KeyWireType != ProtoWireTypes.LengthDelimited)
                    {
                        switch (KeyTypeCode)
                        {
                            case TypeCode.Int32:
                                _binaryWriter.WriteInt32((Int32)_currentValue);
                                goto three;
                            case TypeCode.Int64:
                                _binaryWriter.WriteInt64((Int64)_currentValue);
                                goto three;
                        }
                    }

                    _type = KeyType;
                    _typeCode = KeyTypeCode;
                    _wireType = KeyWireType;
                    _subIndex = 3;


                    return true;
                case 3:
                    three:
                    
                    var v2 = (Int32)ValueWireType + (2 << 3);
                    _binaryWriter.WriteInt32(v2);
                    goto four;
                case 4:
                    four:
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

        public ProtoWireTypes WireType => _wireType;

        public TypeCode TypeCode => _typeCode;

        public Boolean IsRepeatedField => _isRepeated;
        public Object GetValue(Object @from)
        {
            if (!_enumerator.MoveNext())
                return null;
            return _enumerator.Current;
        }

        public Int32 Header => _header;
        String INamedField.Name => throw new NotSupportedException(); //_current.Name;

        public Object Value => _currentValue; 

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
