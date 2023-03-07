#if GENERATECODE

using System;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Das.Serializer.State
{
    public interface IDynamicState<out TField> : IDynamicState
        where TField : IPropertyInfo
    {
        new TField CurrentField { get; }
    }

    public interface IDynamicState
    {
        void LoadCurrentFieldValueToStack();

        LocalBuilder GetLocal(Type localType);

        LocalBuilder GetLocal<T>();

        void LoadParentToStack();

        Label VerifyShouldPrintValue();


        ILGenerator IL { get; }

        IPropertyInfo CurrentField { get; }

        FieldAction CurrentFieldAction { get; }
    }
}


#endif
