#if GENERATECODE

using System;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.State;

namespace Das.Serializer.Proto
{
    public interface IProtoPrintState : IDynamicPrintState<IProtoFieldAccessor, Boolean>,
                                        IProtoState
    {
        Byte[] CurrentFieldHeader { get; }

        LocalBuilder FieldByteArray { get; }

        Boolean IsArrayMade { get; set; }

        LocalBuilder LocalBytes { get; }

        LocalBuilder ChildObjectStream { get; }

        void PrintFieldViaProxy(Action<ILGenerator> loadFieldValue);
    }
}


#endif