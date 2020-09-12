#if !NET40
using System.Runtime.CompilerServices;
#else
using Das.Serializer.ProtoBuf;
#endif


using System;
using System.IO;

using System.Threading.Tasks;


namespace Das.Serializer
{
    public class DasSerializer : DasCoreSerializer
    {
        #if !NET40
        // ReSharper disable once UnusedMember.Global
        public DasSerializer(IStateProvider stateProvider)
            : base(stateProvider, WriteAsync, ReadToEndAsync, ReadAsync)
        {
        }

        public DasSerializer(ISerializerSettings settings)
            : base(new DefaultStateProvider(settings),
                WriteAsync, ReadToEndAsync, ReadAsync)
        {
        }

        public DasSerializer() : base(new DefaultStateProvider(), WriteAsync, ReadToEndAsync, ReadAsync)
        {
        }

        [MethodImpl(256)]
        private static Task WriteAsync(TextWriter writer, String writeThis)
            => writer.WriteAsync(writeThis);

        [MethodImpl(256)]
        private static Task<String> ReadToEndAsync(TextReader reader)
            => reader.ReadToEndAsync();

        [MethodImpl(256)]
        private static Task<Int32> ReadAsync(Stream stream, Byte[] buffer, Int32 offset, Int32 count)
            => stream.ReadAsync(buffer, offset, count);


        #else

        public override IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
            ProtoBufOptions<TPropertyAttribute> options)
        {
            var provider = new ProtoDynamicProvider<TPropertyAttribute>(options,
                TypeManipulator, ObjectInstantiator, ObjectManipulator);
            return new ProtoBufSerializer(StateProvider, Settings, provider);
        }

        // ReSharper disable once UnusedMember.Global
        public DasSerializer(IStateProvider stateProvider,
                             Func<TextWriter, String, Task> writeAsync,
                             Func<TextReader, Task<String>> readToEndAsync,
                             Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : base(stateProvider, writeAsync, readToEndAsync, readAsync)
        {
        }

        public DasSerializer(ISerializerSettings settings,
                             Func<TextWriter, String, Task> writeAsync,
                             Func<TextReader, Task<String>> readToEndAsync,
                             Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : base(new DefaultStateProvider(settings), writeAsync, readToEndAsync, readAsync)
        {
        }

        public DasSerializer(Func<TextWriter, String, Task> writeAsync,
                             Func<TextReader, Task<String>> readToEndAsync,
                             Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : base(new DefaultStateProvider(), writeAsync, readToEndAsync, readAsync)
        {
        }


        #endif
    }
}