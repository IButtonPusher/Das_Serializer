using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Das.Streamers;

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

            var typeStructure = _typeManipulator.GetPrintProtoStructure(typeO, _options);
            var res = _instantiator.BuildDefault<T>(true);
            Object ooutput = res;
            Boolean canContinue;

            do //outer loop for properties that are proto contracts
            {
                canContinue = false;
                Object propValue;

                IProtoFieldAccessor currentProp;
                while (_feeder.HasMoreBytes)
                {
                    _feeder.GetInt32(ref iVal);
                    var wireType = iVal & 7;
                    var columnIndex = iVal >> 3;
                    ///////

                    currentProp = typeStructure.FieldMap[columnIndex];
                    var currentType = currentProp.Type;

                    do //inner loop for collections
                    {
                        switch (wireType)
                        {
                            case Const.VarInt when currentType == Const.IntType:
                                _feeder.GetInt32(ref iVal);
                                typeStructure.SetValue(currentProp.Name, ref ooutput, iVal,
                                    SerializationDepth.GetSetProperties);
                                continue;
                            case Const.VarInt when currentType == Const.ByteType:
                                propValue = _feeder.GetByte();
                                break;
                            case Const.VarInt:
                                propValue = _feeder.GetVarInt(currentType);
                                break;
                            case Const.Int64: //64-bit zb double
                                propValue = _feeder.GetPrimitive(currentType);
                                break;
                            case Const.LengthDelimited when currentProp.IsLeafType:
                                propValue = _feeder.GetPrimitive(currentType);
                                break;
                            case Const.LengthDelimited:
                                _feeder.GetInt32(ref iVal);

                                switch (currentProp.TypeCode)
                                {
                                    case TypeCode.String:
                                        var sBytes = _feeder.GetBytes(iVal);
                                        propValue = Encoding.UTF8.GetString(sBytes);
                                        break;

                                    case TypeCode.Object:

                                        if (currentProp.Type == Const.ByteArrayType)
                                        {
                                            propValue = _feeder.GetBytes(iVal);
                                            break;
                                        }

                                        canContinue = true;
                                        _objects.Push(ooutput);
                                        _protoStructs.Push(typeStructure);
                                        _properties.Push(currentProp);
                                        typeStructure = _typeManipulator.GetScanProtoStructure(
                                            currentProp.Type, _options, iVal);

                                        if (!typeStructure.IsCollection)
                                            ooutput = _instantiator.BuildDefault(currentProp.Type, true);
                                        else
                                        {
                                            wireType = (Int32) ProtoStructure.GetWireType(
                                                typeStructure.Type);
                                            currentType = typeStructure.Type;

                                        }

                                        Feeder.Push(iVal);

                                        continue;
                                    default:
                                        var pBytes = _feeder.GetBytes(iVal);
                                        //propValue = _primitiveScanner.GetValue(pBytes, property.Type);
                                        propValue = _primitiveScanner.GetValue(pBytes, currentProp.Type);
                                        break;
                                }

                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        typeStructure.SetValue(currentProp.Name, ref ooutput, propValue,
                            SerializationDepth.GetSetProperties);

                    } while (typeStructure.IsCollection);

                }

                if (canContinue)
                {
                    propValue = ooutput;
                    ooutput = _objects.Pop();
                    currentProp = _properties.Pop();
                    typeStructure = _protoStructs.Pop();
                    Feeder.Pop();

                    typeStructure.SetValue(currentProp.Name, ref ooutput, propValue,
                        SerializationDepth.GetSetProperties);
                }

            } 
            while (canContinue);

            return res;
        }
      
    }
}
