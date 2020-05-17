using System;
using System.Collections.Generic;

// ReSharper disable All

namespace Das.Serializer.Scanners
{
    /// <summary>
    /// Abstraction for reading one character at a time for a string/builder etc
    /// </summary>
    public interface ITextAccessor
    {
        Char this[Int32 index] { get; }

        Boolean Contains(String str, StringComparison comparison);

        Int32 Length { get; }

        Boolean IsNullOrWhiteSpace();

        String[] Split();

        String[] Split(Char[] separators, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries);

        String[] TrimAndSplit();

        String Remove(ISet<Char> chars);

        String Substring(Int32 start, Int32 length);

        String Substring(Int32 start);

        Boolean Contains(Char c);
        
        Boolean Contains(String str);

        Int32 Count(Char c);

        void CopyTo(
            int sourceIndex,
            char[] destination,
            int destinationIndex,
            int count);
    }
}
