using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IBinaryState : ISerializationState, IBinaryContext
    {
        IBinaryScanner Scanner { get; }
    }
}


//
//
//namespace Das.Serializer
//{
//    public interface IBinaryState<TScanner, TInput> : ISerializationState, IBinaryContext
//        where TScanner : IBinaryScanner, IScannerBase<TInput>
//    {
//        TScanner Scanner { get; }
//    }
//}
