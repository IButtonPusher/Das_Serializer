using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Das.Scanners;
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
            _properties = new Stack<INamedField>();
            _protoStructs = new Stack<IProtoStructure>();
        }

        

        public ProtoFeeder Feeder { get; set; }

        private readonly ProtoBufOptions<TPropertyAttribute> _options;
        private readonly ITypeManipulator _typeManipulator;
        private readonly IInstantiator _instantiator;
        private readonly IBinaryPrimitiveScanner _primitiveScanner;
        private readonly Stack<Object> _objects;
        private readonly Stack<INamedField> _properties;
        
        private readonly Stack<IProtoStructure> _protoStructs;

        public T Deserialize<T>(Stream stream)
        {
            _feeder = Feeder;
            Feeder.SetStream(stream);

            var typeO = typeof(T);

            var typeStructure = _typeManipulator.GetProtoStructure(typeO, _options);
            var res = _instantiator.BuildDefault<T>(true);
            Object ooutput = res;
            Boolean canContinue;

            do
            {
                canContinue = false;
                Object propValue;

                INamedField currentProp;
                while (_feeder.HasMoreBytes)
                {
                    //field index / wire type
                    var propHeader = _feeder.GetInt32();
                    var wireType = propHeader & 7;
                    var columnIndex = propHeader >> 3;
                    ///////

                    currentProp = typeStructure.FieldMap[columnIndex];
                    var currentType = currentProp.Type;
                    

                    switch (wireType)
                    {
                        case Const.VarInt:
                            propValue = _feeder.GetInt32();
                            break;
                        case Const.Int64: //64-bit zb double
                            propValue = _feeder.GetPrimitive(currentType);
                            break;
                        case Const.LengthDelimited when _typeManipulator.IsLeaf(currentType, true):
                            propValue = _feeder.GetPrimitive(currentType);
                            break;
                        case Const.LengthDelimited:
                            var nextBlockSize = _feeder.GetNextBlockSize();
                            
                            var property = typeStructure.FieldMap[columnIndex];
                            var typeCode = Type.GetTypeCode(property.Type);

                            switch (typeCode)
                            {
                                case TypeCode.String:
                                    var sBytes = _feeder.GetBytes(nextBlockSize);
                                    propValue = Encoding.UTF8.GetString(sBytes);
                                    break;
                                case TypeCode.Object:
                                    canContinue = true;
                                    _objects.Push(ooutput);
                                    _protoStructs.Push(typeStructure);
                                    _properties.Push(currentProp);
                                    typeStructure = _typeManipulator.GetProtoStructure(property.Type, _options);

                                    ooutput = _instantiator.BuildDefault(property.Type, true);
                                    Feeder.Push(nextBlockSize);
                                    
                                    continue;
                                default:
                                    var pBytes = _feeder.GetBytes(nextBlockSize);
                                    propValue = _primitiveScanner.GetValue(pBytes, property.Type);
                                    break;
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    typeStructure.SetValue(currentProp.Name, ref ooutput, propValue,
                        SerializationDepth.GetSetProperties);
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
