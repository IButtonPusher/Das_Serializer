using System;
using System.IO;

namespace Das.Serializer
{
    public interface IProtoSerializer
    {
        void ToProtoStream<TObject>(Stream stream, TObject o) 
            where TObject : class;

        TObject FromProtoStream<TObject>(Stream stream)
            where TObject : class;
    }
}
