using System;
using System.Threading.Tasks;

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
