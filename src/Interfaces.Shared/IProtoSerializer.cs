using System;
using System.IO;
using System.Threading.Tasks;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer;

public interface IProtoSerializer : IProtoProvider
{
   TObject FromProtoStream<TObject>(Stream stream)
      where TObject : class;

   void ToProtoStream<TObject>(Stream stream,
                               TObject o)
      where TObject : class;
}