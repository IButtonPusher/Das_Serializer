using System;

namespace Das.Scanners
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