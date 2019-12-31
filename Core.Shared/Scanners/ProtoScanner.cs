using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Scanners
{
    internal class ProtoScanner<TPropertyAttribute> : BinaryScanner
        where TPropertyAttribute : Attribute
    {
        public ProtoScanner(IBinaryContext state, ProtoBufOptions<TPropertyAttribute> options)
            : base(state)
        {
            _options = options;
            _typeManipulator = state.TypeManipulator;
            _instantiator = state.ObjectInstantiator;
            _primitiveScanner = state.PrimitiveScanner;
            _objects = new Stack<Object>();
            _properties = new Stack<IProtoFieldAccessor>();
            _protoStructs = new Stack<IProtoStructure>();
        }

        public ProtoFeeder Feeder { get; set; }

        private readonly ProtoBufOptions<TPropertyAttribute> _options;
        private ProtoFeeder _feeder;
        private readonly ITypeManipulator _typeManipulator;
        private readonly IInstantiator _instantiator;
        private readonly IBinaryPrimitiveScanner _primitiveScanner;
        private readonly Stack<Object> _objects;
        private readonly Stack<IProtoFieldAccessor> _properties;
        
        private readonly Stack<IProtoStructure> _protoStructs;

        public T Deserialize<T>(Stream stream)
        {
            _feeder = Feeder;
            Feeder.SetStream(stream);
            var iVal = 0;

            var typeO = typeof(T);

            var typeStructure = _typeManipulator.GetPrintProtoStructure(typeO, _options, _state);
            
            var res = (T) typeStructure.BuildDefault();
            Object ooutput = res;
            Boolean canContinue;

            do //outer loop for properties that are proto contracts
            {
                canContinue = false;
                Object propValue;

                IProtoFieldAccessor currentProp;
                while (_feeder.HasMoreBytes)
                {
                    //field header to wire/index
                    _feeder.GetInt32(ref iVal);
                    //var wireType = iVal & 7;
                    var columnIndex = iVal >> 3;

                    currentProp = typeStructure.FieldMap[columnIndex];
                    var currentType = currentProp.Type;
                    
                    var wireType = currentProp.WireType;

                    var typeCode = currentProp.TypeCode;

                    do
                    {
                        //inner loop for collections
                        //avoid re-initializing things for the germane type
                        //that won't change

                        switch (wireType)
                        {
                            case ProtoWireTypes.Varint when currentType == Const.IntType:
                                _feeder.GetInt32(ref iVal);
                                typeStructure.SetPropertyValueUnsafe(currentProp.Name,
                                    ref ooutput, iVal);
                                continue;
                            case ProtoWireTypes.Varint when currentType == Const.ByteType:
                                propValue = _feeder.GetByte();
                                break;
                            case Const.VarInt:
                                propValue = _feeder.GetVarInt(currentType);
                                break;
                            case ProtoWireTypes.Int64: //64-bit zb double
                            case ProtoWireTypes.Int32:
                                propValue = _feeder.GetPrimitive(currentType);
                                break;

                            case ProtoWireTypes.LengthDelimited when currentProp.IsLeafType:
                                propValue = _feeder.GetPrimitive(currentType);
                                break;
                            case ProtoWireTypes.LengthDelimited:
                                var columnHeader = iVal;
                                _feeder.GetInt32(ref iVal);

                                switch (typeCode)
                                {
                                    case TypeCode.String:
                                        ///////////
                                        // STRING
                                        ///////////
                                        var sBytes = _feeder.GetBytes(iVal);
                                        propValue = Encoding.UTF8.GetString(sBytes);
                                        break;

                                    case TypeCode.Object:

                                        if (currentProp.Type == Const.ByteArrayType)
                                        {
                                            ////////
                                            // BYTE ARRAY
                                            ////////
                                            propValue = _feeder.GetBytes(iVal);
                                            break;
                                        }

                                        canContinue = true;
                                        _objects.Push(ooutput);
                                        _protoStructs.Push(typeStructure);
                                        _properties.Push(currentProp);
                                         typeStructure = _typeManipulator.GetScanProtoStructure(
                                             currentProp.Type, _options, iVal, _state, _feeder, 
                                             columnHeader);

                                        if (!currentProp.IsRepeated)
                                        {
                                            /////////////////////////
                                            // NESTED REFERENCE TYPE
                                            /////////////////////////
                                            
                                            ooutput = typeStructure.BuildDefault();
                                        }
                                        else
                                        {
                                            ////////////////////////////////
                                            // REPEATED FIELD -> *COLLECTION* 
                                            ////////////////////////////////
                                            Feeder.Push(iVal);

                                            _feeder.GetInt32(ref iVal);
                                            wireType = (ProtoWireTypes) (iVal & 7);
                                            columnIndex = iVal >> 3;
                                            currentType = typeStructure.Type;

                                            continue;
                                        }

                                        Feeder.Push(iVal);

                                        continue;
                                    default:
                                        var pBytes = _feeder.GetBytes(iVal);
                                        propValue = _primitiveScanner.GetValue(pBytes, currentProp.Type);
                                        break;
                                }

                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        typeStructure.SetPropertyValueUnsafe(currentProp.Name, ref ooutput,
                            propValue);

                    } 
                    while (typeStructure.IsRepeating(ref wireType, ref typeCode, ref currentType));


                }

                if (canContinue)
                {
                    propValue = ooutput;
                    ooutput = _objects.Pop();
                    currentProp = _properties.Pop();
                    typeStructure = _protoStructs.Pop();
                    Feeder.Pop();

                    typeStructure.SetPropertyValueUnsafe(currentProp.Name, ref ooutput, propValue);
                }

            } 
            while (canContinue);

            return res;
        }
      
    }
}
