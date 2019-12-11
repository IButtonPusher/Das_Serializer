using System;
using System.IO;

namespace Das.Serializer
{
    public interface IProtoSerializer
    {
        void ToProtoStream<TObject>(Stream stream, TObject o);

        TObject FromProtoStream<TObject>(Stream stream);
    }
}
