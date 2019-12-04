using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Das.Serializer;
using Das.Serializer.Objects;
using Serializer.Core.Printers;
using Serializer.Core.Remunerators;

namespace Das.Printers
{
    internal class ProtoPrinter<TPropertyAttribute> : BinaryPrinter
        where  TPropertyAttribute : Attribute
    {
        private readonly ITypeManipulator _typeManipulator;
        private readonly ProtoBufOptions<TPropertyAttribute> _protoSettings;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly ConcurrentDictionary<Type, Int32> _wireTypes;

        static ProtoPrinter()
        {
            var wireTypes = new Dictionary<Type, Int32>
            {
                {typeof(Int32), 0},
                {typeof(Int64), 0},
                {typeof(UInt32), 0},
                {typeof(UInt64), 0},
                {typeof(Boolean), 0},
                {typeof(Enum), 0},
                {typeof(Double), 1},
                {typeof(String), 2},
                {typeof(Byte[]), 2},
                {typeof(Single), 5}
            };

            _wireTypes = new ConcurrentDictionary<Type, Int32>(wireTypes);
        }

        public ProtoPrinter(IBinaryWriter writer, IBinaryState stateProvider,
            ITypeManipulator typeManipulator, ProtoBufOptions<TPropertyAttribute> protoSettings)
            : base(writer, stateProvider)
        {
            _typeManipulator = typeManipulator;
            _protoSettings = protoSettings;
        }

        public override Boolean PrintNode(NamedValueNode node)
        {
            var res = _stateProvider.GetNodeType(node.Type, Settings.SerializationDepth);
            var print = new PrintNode(node, res);

            switch (node)
            {
                case PropertyValueNode prop:
                    var typeStructure = _typeManipulator.GetTypeStructure(prop.DeclaringType, Settings);

                    if (!typeStructure.TryGetAttribute<TPropertyAttribute>(node.Name, out var attributes))
                        return false;
                    PrintWireType(node, attributes[0]);
                    var isLeaf = IsLeaf(node.Type, true);

                    return PrintBinaryNode(print, !isLeaf);
                default:
                    return PrintObject(print);
            }
           
            //return PrintObject(print);
        }

        protected sealed override void WriteString(String str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var len = bytes.Length;
            
            _bWriter.WriteInt32(len);
            _bWriter.Append(bytes);
        }

        private void PrintWireType(ValueNode node, TPropertyAttribute attribute)
        {
            if (!_wireTypes.TryGetValue(node.Type, out var wire))
            {
                if (node.Value is Enum)
                    wire = 0;
                else wire = 2;

                _wireTypes[node.Type] = wire;
            }

            var index = _protoSettings.GetIndex(attribute);
            wire += (index << 3);
            _bWriter.WriteInt32(wire);

        }
    }
}
