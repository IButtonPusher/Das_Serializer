using System;
using Das.Serializer.ObjectPools;

namespace Das.Serializer.Remunerators
{
    public class DeferredProtoWriterPool : ObjectLender<DeferredProtoWriter, ProtoBufWriter>
    {
        protected override Func<ProtoBufWriter, DeferredProtoWriter> GetNew { get; }

        public DeferredProtoWriterPool()
        {
            GetNew = parent => new DeferredProtoWriter(parent);
        }
    }
}
