using System;
using System.Collections;
using System.Collections.Generic;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoDictionaryScanner : IProtoScanStructure
    {
        public ProtoDictionaryScanner(IProtoStructure protoStruct, IProtoFeeder byteFeeder,
            Int32 fieldHeader)
        {
            var gargs = protoStruct.Type.GetGenericArguments();
            KeyType = gargs[0];
            ValueType = gargs[1];

            KeyWireType = ProtoStructure.GetWireType(KeyType);
            ValueWireType = ProtoStructure.GetWireType(ValueType);
            
            KeyTypeCode = Type.GetTypeCode(KeyType);
            ValueTypeCode = Type.GetTypeCode(ValueType);

            _protoStruct = protoStruct;
            _byteFeeder = byteFeeder;
            _fieldHeader = fieldHeader;

            _value = (IDictionary)protoStruct.BuildDefault();
        }

        public void Set(IProtoFeeder byteFeeder, Int32 fieldHeader)
        {
            _byteFeeder = byteFeeder;
            _fieldHeader = fieldHeader;
            _value = (IDictionary)_protoStruct.BuildDefault();
            _scanIndex = 0;
        }

        protected IDictionary _value;
        private Int32 _scanIndex;
        
        private Object _keyVal;

        private Boolean _isLastValueMeta;
        private readonly IProtoStructure _protoStruct;
        private IProtoFeeder _byteFeeder;
        private  Int32 _fieldHeader;
        private Int32 _propHeader;
        

        private readonly Type KeyType;
        private readonly Type ValueType;

        private readonly TypeCode KeyTypeCode;

        private readonly TypeCode ValueTypeCode;

        private readonly ProtoWireTypes KeyWireType;

        public ProtoWireTypes ValueWireType { get; }

        public Object BuildDefault()
        {
            return _protoStruct.BuildDefault();
        }

        public Dictionary<Int32, IProtoFieldAccessor> FieldMap => _protoStruct.FieldMap;

        public Boolean IsRepeating(ref ProtoWireTypes wireType, ref TypeCode typeCode,
            ref Type type)
        {
            switch (_scanIndex)
            {
                case 1 when _isLastValueMeta:
                    //_byteFeeder.GetInt32(ref _propHeader);
                    _byteFeeder.DumpInt32();
                    _isLastValueMeta = false;
                    goto oneNotMeta;

                case 1 when !_isLastValueMeta:
                    oneNotMeta:
                    wireType = ValueWireType;
                    typeCode = ValueTypeCode;
                    type = ValueType;
                    return true;
                
                case 0:

                    wireType = KeyWireType;
                    typeCode = KeyTypeCode;
                    type = KeyType;

                    if (_byteFeeder.HasMoreBytes) 
                        return true;

                    var peek = _byteFeeder.PeekInt32(_fieldHeader);

                    if (peek != _fieldHeader)
                        return false;

                    var len = _byteFeeder.GetInt32();
                    _byteFeeder.Pop();
                    _byteFeeder.Push(len);
                    
                    _byteFeeder.DumpInt32();
                    //_byteFeeder.GetInt32(ref _propHeader);

                    return true;
            }

            return true;
        }

        // ReSharper disable once RedundantAssignment
        public Type Type => _protoStruct.Type;

        void ITypeStructureBase.SetPropertyValueUnsafe(String propName, ref Object targetObj, 
            Object propVal)
        {
            if (_isLastValueMeta)
            {
                _isLastValueMeta = false;
                return;
            }

            targetObj = _value;


            var rdrr = _scanIndex++;

                switch (rdrr)
                {
                    case 0:
                        _keyVal = propVal;
                        _isLastValueMeta = true;
                        break;
                    case 1:
                        _value.Add(_keyVal, propVal);
                        _scanIndex = 0;
                        break;
                }
        }
    }
}
