using System;
using Das.Serializer.Scanners;

namespace Das.Serializer
{
    public interface ITextScanner : IScannerBase<String>,
        //IScannerBase<IEnumerable<Char>>, 
        IScannerBase<Char[]>,
        ISerializationDepth
    {
        [NotNull]
        ITextNode RootNode { get; }
    }
}