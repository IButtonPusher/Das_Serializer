namespace Das.Serializer.Scanners
{
    public interface IScannerBase<in TInput> : IScannerBase
    {
        TOutput Deserialize<TOutput>(TInput source);
    }

    public interface IScannerBase
    {
        void Invalidate();
    }
}