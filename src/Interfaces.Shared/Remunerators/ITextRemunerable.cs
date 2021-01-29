using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public interface ITextRemunerable : IRemunerable<String, Char>, IStringRemunerable
    {
        Int32 Capacity { get; set; }

        Int32 Length { get; }

        void Append(Char data1,
                    String data2);

        void Append(ITextAccessor txt);

        new void Append(Char item);

        Boolean Append<T>(IEnumerable<T> items,
                          Char separator)
            where T : IConvertible;

        void Insert(Int32 index,
                    String str);

        void Remove(Int32 startIndex,
                    Int32 length);

        ITextAccessor ToImmutable();

        void Undispose();
    }

    public interface IStringRemunerable : IRemunerable<String>
    {
        Char this[Int32 index] { get; }

        String this[Int32 start,
                    Int32 end] { get; }

        new void Append(String data);

        void Append(String data1,
                    String data2);

        void Append(IEnumerable<String> datas);

        void Append<T>(T data) where T : struct;

        void Clear();

        Int32 IndexOf(String str,
                      Int32 startIndex);
    }
}
