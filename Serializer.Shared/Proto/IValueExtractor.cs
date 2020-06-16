using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Das.Serializer.ProtoBuf;

namespace Das.Serializer.Proto
{
    public interface IValueExtractor
    {
        void LoadNextString();

        IDictionary<IProtoFieldAccessor, FieldBuilder> ChildProxies { get; }

        FieldBuilder GetProxy(Type type);

        ILGenerator IL { get; }

        LocalBuilder LastByteLocal { get; }
    }
}