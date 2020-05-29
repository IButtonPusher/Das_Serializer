using System;
using System.IO;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer
{
    public interface IProtoSerializer : IProtoProvider
    {
        void ToProtoStream<TObject>(Stream stream, TObject o) 
            where TObject : class;

        TObject FromProtoStream<TObject>(Stream stream)
            where TObject : class;

    }
}
