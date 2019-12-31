using System;
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

        void CopyTo(
            int sourceIndex,
            char[] destination,
            int destinationIndex,
            int count);
    }
}
