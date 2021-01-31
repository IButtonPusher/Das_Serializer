using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    /// <summary>
    ///     Stateless facade for text based deserialization
    /// </summary>
    public interface ITextContext : ISerializationContext
    {
        IStringPrimitiveScanner PrimitiveScanner { get; }

        ITextNodeProvider ScanNodeProvider { get; }

        INodeSealer<ITextNode> Sealer { get; }
    }
}
