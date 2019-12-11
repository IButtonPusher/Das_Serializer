using System;
using System.IO;
using System.Text;
using Das.Serializer;
using Das.Serializer.Objects;
using Das.Serializer.Remunerators;

namespace Das.Printers
{
    internal class ProtoPrinter<TPropertyAttribute> : BinaryPrinter
        where  TPropertyAttribute : Attribute
    {
        private readonly ProtoBufWriter _writer;
        private readonly ITypeManipulator _types;
        private readonly ProtoBufOptions<TPropertyAttribute> _protoSettings;

        public ProtoPrinter(ProtoBufWriter writer, IBinaryState stateProvider,
            ITypeManipulator typeManipulator, ProtoBufOptions<TPropertyAttribute> protoSettings)
            : base(writer, stateProvider)
        {
            _writer = writer;
            _types = typeManipulator;
            _protoSettings = protoSettings;
        }

        public void Print<TObject>(TObject o)
        {
            var typeO = typeof(TObject);
            var typeStructure = _types.GetProtoStructure(typeO, _protoSettings);
            var properyValues = typeStructure.GetPropertyValues(o, this);
            for (var c = 0; c < properyValues.Count; c++)
            {
                //header
                var pv = properyValues[c];
                if (!typeStructure.TryGetHeader(pv, out var header))
                    continue;

                _bWriter.WriteInt32(header);
                /////

                var code = Type.GetTypeCode(pv.Type);
                if (Print(pv.Value, code))
                    continue;

                using (var print = _printNodePool.GetPrintNode(pv))
                    PrintBinaryNode(print, true);
            }
        }

        public Stream Stream
        {
            // ReSharper disable once UnusedMember.Global
            get => _writer.OutStream;
            set => _writer.OutStream = value;
        }

        public override Boolean PrintNode(INamedValue node)
        {
            using (var print = _printNodePool.GetPrintNode(node))
            {
                switch (node)
                {
                    case IProperty prop:
                        if (!TryPrintHeader(prop))
                            return false;

                        var isLeaf = _typeInferrer.IsLeaf(node.Type, true);

                        return PrintBinaryNode(print, !isLeaf);
                    default:
                        return PrintObject(print);
                }
            }
        }

        protected sealed override void WriteString(String str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var len = bytes.Length;
            
            _bWriter.WriteInt32(len);
            _bWriter.Append(bytes);
        }

        private Boolean TryPrintHeader(INamedField prop, IProtoStructure typeStructure)
        {
            if (!typeStructure.TryGetHeader(prop, out var header))
                return false;
            
            _bWriter.WriteInt32(header);
            return true;
        }

        private Boolean TryPrintHeader(IProperty prop)
        {
            var typeStructure = _types.GetProtoStructure(prop.DeclaringType, _protoSettings);
            return TryPrintHeader(prop, typeStructure);
        }
    }
}
