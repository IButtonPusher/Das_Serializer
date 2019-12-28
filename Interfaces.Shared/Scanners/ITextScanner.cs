using System;
using System.Collections.Generic;
using Das.Serializer.Scanners;

namespace Das.Serializer
{
    public interface ITextScanner : IScannerBase<IEnumerable<Char>>, IScannerBase<Char[]>,
        ISerializationDepth
    {
        [NotNull]
        ITextNode RootNode { get; }
    }
}