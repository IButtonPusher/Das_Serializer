using Das.Serializer.ProtoBuf;
using System;
using System.Threading.Tasks;

#if !NET40

#else
using Das.Serializer.ProtoBuf;
using System;
using System.IO;
using System.Threading.Tasks;
#endif


namespace Das.Serializer
{
    public class DasSerializer : DasCoreSerializer
    {
        #if !NET40
        // ReSharper disable once UnusedMember.Global
        public DasSerializer(IStateProvider stateProvider)
            : base(stateProvider, DasSettings.CloneDefault(), 
                WriteAsync, ReadToEndAsync, ReadAsync)
        {
        }

        public DasSerializer(ISerializerSettings settings)
            : base(new DefaultStateProvider(settings), settings,
                WriteAsync, ReadToEndAsync, ReadAsync)
        {
        }

        public DasSerializer() : 
            base(new DefaultStateProvider(), DasSettings.CloneDefault(),
                WriteAsync, ReadToEndAsync, ReadAsync)
        {
        }


        #else
        //public override IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
        //    IProtoBufOptions<TPropertyAttribute> options)
        //{
        //    var provider = new ProtoDynamicProvider<TPropertyAttribute>(options,
        //        TypeManipulator, ObjectInstantiator, ObjectManipulator);
        //    return new ProtoBufSerializer(StateProvider, Settings, provider);
        //}

        // ReSharper disable once UnusedMember.Global
        public DasSerializer(IStateProvider stateProvider,
                             Func<TextWriter, String, Task> writeAsync,
                             Func<TextReader, Task<String>> readToEndAsync,
                             Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : base(stateProvider, DasSettings.CloneDefault(),
                writeAsync, readToEndAsync, readAsync)
        {
        }

        public DasSerializer(ISerializerSettings settings,
                             Func<TextWriter, String, Task> writeAsync,
                             Func<TextReader, Task<String>> readToEndAsync,
                             Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : base(new DefaultStateProvider(settings), settings, writeAsync, readToEndAsync, readAsync)
        {
        }

        public DasSerializer(Func<TextWriter, String, Task> writeAsync,
                             Func<TextReader, Task<String>> readToEndAsync,
                             Func<Stream, Byte[], Int32, Int32, Task<Int32>> readAsync)
            : base(new DefaultStateProvider(), DasSettings.CloneDefault(),
                writeAsync, readToEndAsync, readAsync)
        {
        }

        #endif

        #if GENERATECODE

        /// <summary>
        /// Returns a protocol buffers serializer that uses TPropertyAttribute as the attribute
        /// for determining member tags/indexes
        /// </summary>
        /// <seealso cref="ProtoBufOptions.Default">Default implementation that uses
        /// Das.Serializer.IndexedMemberAttribute and its 'Index' Property</seealso>
        public override IProtoSerializer GetProtoSerializer<TPropertyAttribute>(
            IProtoBufOptions<TPropertyAttribute> options)
        {
            var protoProv = new ProtoDynamicProvider<TPropertyAttribute>(options, TypeManipulator,
                ObjectInstantiator, ObjectManipulator);

            return new ProtoBufSerializer(StateProvider, Settings,
                protoProv);
        }

        #endif
    }
}
