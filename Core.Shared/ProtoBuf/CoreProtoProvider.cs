using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
