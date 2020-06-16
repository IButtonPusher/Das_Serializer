using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITextRemunerable : IRemunerable<String, Char>, IStringRemunerable
    {
        Int32 Length { get; }

        void Append(Char data1, String data2);

        void Append(ITextAccessor txt);

        new void Append(Char item);

        Boolean Append<T>(IEnumerable<T> items, Char separator)
            where T : IConvertible;

        void Remove(Int32 startIndex, Int32 length);

        ITextAccessor ToImmutable();

        void Undispose();
    }

    public interface IStringRemunerable : IRemunerable<String>
    {
        new void Append(String data);

        void Append(String data1, String data2);

        void Append(IEnumerable<String> datas);

        void Append<T>(T data) where T : struct;
    }
}