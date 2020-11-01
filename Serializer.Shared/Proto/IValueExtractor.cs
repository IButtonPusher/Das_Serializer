#if GENERATECODE

using System;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Das.Serializer.Proto
{
    public interface IValueExtractor
    {
        ILGenerator IL { get; }

        LocalBuilder LastByteLocal { get; }

        FieldBuilder GetProxy(Type type);

        void LoadNextString();
    }
}

#endif