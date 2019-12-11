using System;

namespace Das.Serializer
{
    public interface IDataLender<T> where T: ILendable<T>
    {
        T Get();

        void Put(T item);
    }

    public interface IDataLender<T, in TParam> where T : ILendable<T>
    {
        T Get(TParam input);

        void Put(T item);
    }

    public interface IDataLender<out T, in TParam1, in TParam2> 
        where T : ILendable<T>
    {
        T Get(TParam1 input1, TParam2 input2);
    }

    public interface IDataLender<out T, in TParam1, in TParam2, in TParam3>
        where T : ILendable<T>
    {
        T Get(TParam1 input1, TParam2 input2, TParam3 input3);
    }
}