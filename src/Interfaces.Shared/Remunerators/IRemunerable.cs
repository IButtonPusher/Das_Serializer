using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface IRemunerable<in T, in TE> : IRemunerable<T>,
                                                IDisposable 
        where T : IEnumerable<TE>
    {
        void Append(TE data);
    }

    public interface IRemunerable<in T> : IRemunerable
    {
        void Append(T data);

        void Append(T data,
                    Int32 limit);
    }

    public interface IRemunerable
    {
        Boolean IsEmpty { get; }
    }
}
