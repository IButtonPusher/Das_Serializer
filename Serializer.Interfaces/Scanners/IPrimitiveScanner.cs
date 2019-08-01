using System;

namespace Das.Scanners
{
    public interface IPrimitiveScanner<in T>
    {
        //IScannerBase Scanner { get; set; }

        //TResult GetValue<TResult>(T input);

        Object GetValue(T input, Type type);
    }
}