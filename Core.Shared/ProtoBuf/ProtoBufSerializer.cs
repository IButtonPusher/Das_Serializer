using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Das.Serializer.ProtoBuf
{
    // public class ProtoBufSerializer<TPropertyAttribute> 
    //     : ProtoBufSerializer, IProtoSerializer
    //     where  TPropertyAttribute : Attribute
    // {
    //     public ProtoBufSerializer(IStateProvider stateProvider, ISerializerSettings settings, 
    //         IProtoProvider typeProvider) 
    //         : base(stateProvider, settings)
    //     {
    //         StateProvider = stateProvider;
    //         TypeProvider = typeProvider;
    //     }
    //
    //     public IStateProvider StateProvider { get; }
    //
    //     protected IProtoProvider TypeProvider;
    //
    //     public void ToProtoStream<TObject>(Stream stream, TObject o)
    //         where TObject : class
    //     {
    //         var printer = TypeProvider.GetProtoProxy<TObject>();
    //         printer.OutStream = stream;
    //         printer.Print(o);
    //     }
    //
    //     public TObject FromProtoStream<TObject>(Stream stream)
    //         where TObject : class
    //     {
    //         var scanner = TypeProvider.GetProtoProxy<TObject>();
    //
    //         return scanner.Scan(stream);
    //     }
    //
    //     public override IScanNodeProvider ScanNodeProvider
    //         => StateProvider.BinaryContext.ScanNodeProvider;
    // }

    public class ProtoBufSerializer : CoreContext, IProtoSerializer
    {
        private static readonly ConcurrentDictionary<Type, ProtoWireTypes> _wireTypes;
        private static readonly Type _enumType = typeof(Enum);

        static ProtoBufSerializer()
        {
            var wireTypes = new Dictionary<Type, ProtoWireTypes>
            {
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
            //ISerializationCore dynamicFacade, 
            ISerializerSettings settings,
            IProtoProvider typeProvider) 
            : base(stateProvider, settings)
        {
            StateProvider = stateProvider;
            TypeProvider = typeProvider;
        }

        public IStateProvider StateProvider { get; }

        protected IProtoProvider TypeProvider;

        public void ToProtoStream<TObject>(Stream stream, TObject o)
            where TObject : class
        {
            var printer = TypeProvider.GetProtoProxy<TObject>();
            printer.OutStream = stream;
            printer.Print(o);
        }

        public TObject FromProtoStream<TObject>(Stream stream)
            where TObject : class
        {
            var scanner = TypeProvider.GetProtoProxy<TObject>();

            return scanner.Scan(stream);
        }

        public override IScanNodeProvider ScanNodeProvider
            => StateProvider.BinaryContext.ScanNodeProvider;

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
    }
}
