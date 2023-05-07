#if GENERATECODE

using System;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer.ProtoBuf;
using Das.Serializer.State;

namespace Das.Serializer.Proto
{
    public interface IProtoScanState : IDynamicState<IProtoFieldAccessor>,
                                       IValueExtractor
    {
        Action<IProtoFieldAccessor, IProtoScanState> GetFieldSetInit(IProtoFieldAccessor field,
                                                                     Boolean canSetValueInline);

        Action<IProtoFieldAccessor, IProtoScanState> GetFieldSetCompletion(IProtoFieldAccessor field,
                                                                           Boolean canSetValueInline,
                                                                           Boolean isValuePreInitialized);

        IProtoFieldAccessor[] Fields { get; }

        void LoadPositiveInt32();

        Boolean TryGetAdderForField(IProtoFieldAccessor field,
                                    out MethodInfo adder);

        void EnsureLocalFields();

        new IProtoFieldAccessor CurrentField { get; set; }
    }
}

#endif