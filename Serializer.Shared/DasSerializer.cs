using System;
using System.IO;
using System.Threading.Tasks;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public class DasSerializer : DasCoreSerializer
    {
#if !NET40

        // ReSharper disable once UnusedMember.Global
        public DasSerializer(IStateProvider stateProvider)
            : base(stateProvider, WriteAsync, ReadToEndAsync)
        {
        }

        public DasSerializer(ISerializerSettings settings)
            : base(new DefaultStateProvider(settings),
                WriteAsync, ReadToEndAsync)
        {
        }

        public DasSerializer() : base(new DefaultStateProvider(), WriteAsync, 
            ReadToEndAsync)
        {
        }

        private static async Task WriteAsync(TextWriter writer, String writeThis)
            => await writer.WriteAsync(writeThis);

        private static async Task<String> ReadToEndAsync(TextReader reader)
            => await reader.ReadToEndAsync();


        public override IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
            ProtoBufOptions<TPropertyAttribute> options)
        {
            var provider = new ProtoDynamicProvider<TPropertyAttribute>(options,
                TypeManipulator, ObjectInstantiator);
            return new ProtoBufSerializer<TPropertyAttribute>(StateProvider, Settings, provider);
        }

#else

        public DasSerializer(IStateProvider stateProvider, 
            Func<TextWriter, String, Task> writeAsync,
            Func<TextReader, Task<String>> readToEndAsync)
            : base(stateProvider, writeAsync, readToEndAsync)
        {
        }

        public DasSerializer(ISerializerSettings settings,
            Func<TextWriter, String, Task> writeAsync,
            Func<TextReader, Task<String>> readToEndAsync)
            : base(new DefaultStateProvider(settings), writeAsync, readToEndAsync)
        {
        }

        public DasSerializer(Func<TextWriter, String, Task> writeAsync,
            Func<TextReader, Task<String>> readToEndAsync) 
            : base(new DefaultStateProvider(), writeAsync, readToEndAsync)
        {
        }



#endif


    }
}