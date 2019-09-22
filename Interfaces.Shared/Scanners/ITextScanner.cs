using System;
using System.Collections.Generic;
using Das.Scanners;

namespace Das.Serializer
{
    public interface ITextScanner : IScannerBase<IEnumerable<Char>>
    {
        ITextNode RootNode { get; }
    }
}