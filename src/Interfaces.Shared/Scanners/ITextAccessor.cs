using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable All

namespace Das.Serializer
{
    /// <summary>
    ///     Abstraction for reading one character at a time for a string/builder etc
    /// </summary>
    public interface ITextAccessor
    {
        Char this[Int32 index] { get; }

        Int32 Length { get; }

        Boolean Contains(String str, StringComparison comparison);

        Boolean Contains(Char c);

        Boolean Contains(String str);

        void CopyTo(
            int sourceIndex,
            char[] destination,
            int destinationIndex,
            int count);

        Int32 Count(Char c);

        Boolean IsNullOrWhiteSpace();

        String Remove(ISet<Char> chars);

        String[] Split();

        String[] Split(Char[] separators, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries);

        String Substring(Int32 start, Int32 length);

        String Substring(Int32 start);

        String[] TrimAndSplit();
    }
}