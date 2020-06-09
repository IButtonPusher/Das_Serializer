using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer.ProtoBuf
{
    public class ProtoBufSerializer : CoreContext, IProtoSerializer
    {
        static ProtoBufSerializer()
        {
            var wireTypes = new Dictionary<Type, ProtoWireTypes>
            {
                {typeof(Int16), ProtoWireTypes.Varint},
                {typeof(Int32), ProtoWireTypes.Varint},
                {typeof(Int64), ProtoWireTypes.Varint},
                {typeof(UInt32), ProtoWireTypes.Varint},
                {typeof(UInt64), ProtoWireTypes.Varint},
                {typeof(Byte), ProtoWireTypes.Varint},
                {typeof(Boolean), ProtoWireTypes.Varint},
                {typeof(Enum), ProtoWireTypes.Varint},
                {typeof(Double), ProtoWireTypes.Int64},
                {typeof(String), ProtoWireTypes.LengthDelimited},
                {typeof(Byte[]), ProtoWireTypes.LengthDelimited},
                {typeof(Single), ProtoWireTypes.Int32}
            };

            _wireTypes = new ConcurrentDictionary<Type, ProtoWireTypes>(wireTypes);
        }

        public ProtoBufSerializer(IStateProvider stateProvider,
            ISerializerSettings settings,
            IProtoProvider typeProvider)
            : base(stateProvider, settings)
        {
            StateProvider = stateProvider;
            TypeProvider = typeProvider;
        }

        public void ToProtoStream<TObject>(Stream stream, TObject o)
            where TObject : class
        {
            var printer = TypeProvider.GetProtoProxy<TObject>(true);
            //printer.OutStream = stream;
            printer.Print(o, stream);
        }

        public TObject FromProtoStream<TObject>(Stream stream)
            where TObject : class
        {
            var scanner = TypeProvider.GetProtoProxy<TObject>();
            return scanner.Scan(stream);
        }


        public IProtoProxy<T> GetProtoProxy<T>(Boolean allowReadOnly = false) where T : class
        {
            var proxy = TypeProvider.GetProtoProxy<T>(allowReadOnly);
            return proxy;
        }

        public bool TryGetProtoField(PropertyInfo prop, Boolean isRequireAttribute, 
            out IProtoFieldAccessor field)
        {
            return TypeProvider.TryGetProtoField(prop, isRequireAttribute, out field);
        }


        public ProtoFieldAction GetProtoFieldAction(Type pType)
        {
            return TypeProvider.GetProtoFieldAction(pType);
        }

        #if DEBUG

        public void DumpProxies()
        {
            TypeProvider.DumpProxies();
        }

#endif


        public override IScanNodeProvider ScanNodeProvider
            => StateProvider.BinaryContext.ScanNodeProvider;

        public IStateProvider StateProvider { get; }

        public static ProtoWireTypes GetWireType(Type type)
        {
            if (!_wireTypes.TryGetValue(type, out var wire))
            {
                if (_enumType.IsAssignableFrom(type))
                    wire = 0;
                else wire = ProtoWireTypes.LengthDelimited;

                _wireTypes[type] = wire;
            }

            return wire;
        }

        private static readonly ConcurrentDictionary<Type, ProtoWireTypes> _wireTypes;
        private static readonly Type _enumType = typeof(Enum);

        protected readonly IProtoProvider TypeProvider;
    }
}