using System;
using System.Collections.Generic;
using Das.Scanners;
using Das.Serializer.Annotations;

namespace Das.Serializer
{
    public interface ITextScanner : IScannerBase<IEnumerable<Char>>, IScannerBase<Char[]>,
        ISerializationDepth
    {
        [NotNull]
        ITextNode RootNode { get; }
    }
}