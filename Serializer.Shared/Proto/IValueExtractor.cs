using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Das.Serializer.Proto
{
    public interface IValueExtractor
    {
        void LoadNextString();

        FieldBuilder GetProxy(Type type);

        ILGenerator IL { get; }

        LocalBuilder LastByteLocal { get; }
    }
}