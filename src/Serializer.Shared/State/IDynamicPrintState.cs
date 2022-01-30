#if GENERATECODE

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Das.Serializer.Properties;

namespace Das.Serializer.State
{
    public interface IDynamicPrintState : IDynamicState
    {
        void AppendPrimitive<TData>(TData data,
                                    TypeCode typeCode,
                                Action<IDynamicPrintState, TData> loadValue);

        void AppendBoolean();

        void AppendChar();

        void AppendChar(Char c);

        void AppendInt8();

        void AppendInt16();

        void AppendUInt16();

        void AppendInt32();

        void AppendUInt32();

        void AppendInt64();

        void AppendUInt64();


        void AppendSingle();

        void AppendDouble();

        void AppendDecimal();

        void AppendDateTime();

        //void AppendValue<TData>(TData data,
        //                        Action<IDynamicState, TData> loadValue);


        void AppendNull();

        IFieldActionProvider ActionProvider { get; }
    }

    public interface IDynamicPrintState<out TField> : IDynamicState<TField>,
                                                      IDynamicPrintState
        where TField : IPropertyInfo
    {
        void PrintCurrentFieldHeader();

        void PrepareToPrintValue();

        //void LoadWriter();
    }

    public interface IDynamicPrintState<out TField, out TReturns> : IDynamicPrintState<TField>,
                                                                    IEnumerable<IDynamicPrintState<TField, TReturns>>
        where TField : IPropertyInfo
    {
        void  PrintChildObjectField(Action<ILGenerator> loadObject,
                               Type fieldType);

        void WriteInt32();

        //void PrintPrimitive();

        void PrintVarIntField();

        void PrintStringField();

        void PrintByteArrayField();

        void PrintDateTimeField();

        void PrintObjectArray();

        void PrintPrimitiveArray();

        void PrintIntCollection();

        void PrintFallback();

        void PrintEnum();

        void PrepareToPrintValue<TData>(TData data, 
                                        Action<IDynamicPrintState, TData> loadValue);


        void PrintObjectCollection();

        void PrintPrimitiveCollection();

        void PrintDictionary();

       
    }
}

#endif
