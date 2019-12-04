using System;
using Das.Scanners;
using Das.Streamers;
using Serializer.Core;

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
        }

        private readonly ProtoBufOptions<TPropertyAttribute> _options;
        private readonly ITypeManipulator _typeManipulator;
        private readonly IInstantiator _instantiator;

        public override T Deserialize<T>(IBinaryFeeder source)
        {
            _feeder = source;
            var root = _nodes.Get(String.Empty, NullNode.Instance, typeof(T));
            BuildReferenceObject(ref root);

            if (root.Value != null)
                _state.ObjectInstantiator.OnDeserialized(root.Value, Settings);

            return (T) root.Value;
        }

        protected override void BuildReferenceObject(ref IBinaryNode node)
        {
            var typeStructure = _typeManipulator.GetProtoStructure(node.Type, _options);
            var output = _instantiator.BuildDefault(node.Type, true);
            var ooutput = output;
            node.Value = ooutput;

            while (_feeder.HasMoreBytes)
            {
                var propHeader = _feeder.GetInt32();
                var wireType = propHeader & 7;
                var columnIndex = (propHeader >> 3);
                
                var currentProp = typeStructure.FieldMap[columnIndex];
                var currentType = currentProp.Type;
                Object propValue;

                switch (wireType)
                {
                    case 0: //Varint
                        propValue = _feeder.GetInt32();
                        break;
                    case 1: //64-bit zb double
                        propValue = _feeder.GetPrimitive(currentType);
                        break;
                    case 2 when _typeManipulator.IsLeaf(currentType, true):
                        propValue = _feeder.GetPrimitive(currentType);
                        break;
                    case 2:
                        var child = _nodes.Get(currentProp.Name, node, currentType);
                        child.BlockSize = _feeder.GetNextBlockSize();

                        var objectsBytes = _feeder.GetBytes(child.BlockSize);
                        var byteArr = new ByteArray(objectsBytes);
                        var f = new ProtoFeeder(_state.PrimitiveScanner, _state, byteArr, Settings,
                            _state.Logger);
                        var hold = _feeder;
                        _feeder = f;
                        BuildNext(ref child);
                        _feeder = hold;
                        propValue = child.Value;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                typeStructure.SetValue(currentProp.Name, ref ooutput, propValue,
                    SerializationDepth.GetSetProperties);
            }
        }
    }
}
