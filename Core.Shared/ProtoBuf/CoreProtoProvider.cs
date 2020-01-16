using System;

namespace Das.Serializer.ProtoBuf
{
    public class CoreProtoProvider : IProtoProvider
    {
        public IProtoProxy<T> GetProtoProxy<T>() where T : class
        {
            throw new NotImplementedException();
        }
    }
}
