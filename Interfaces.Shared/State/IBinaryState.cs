namespace Das.Serializer
{
    public interface IBinaryState: ISerializationState, IBinaryContext
    {
        IBinaryScanner Scanner { get; }
    }
}


//using Das.Serializer.Scanners;
//
//namespace Das.Serializer
//{
//    public interface IBinaryState<TScanner, TInput> : ISerializationState, IBinaryContext
//        where TScanner : IBinaryScanner, IScannerBase<TInput>
//    {
//        TScanner Scanner { get; }
//    }
//}