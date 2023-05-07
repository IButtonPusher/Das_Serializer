#if GENERATECODE

using System;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.CodeGen;

namespace Das.Serializer.ProtoBuf
{
    public interface IValueExtractor
    {
        //ILGenerator IL { get; }

        LocalBuilder LastByteLocal { get; }

        ProxiedInstanceField GetProxy(Type type);

        void LoadNextString();
    }
}

#endif
