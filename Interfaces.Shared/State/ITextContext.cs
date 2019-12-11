namespace Das.Serializer
{
    /// <summary>
    /// Stateless facade for text based deserialization
    /// </summary>
    public interface ITextContext : ISerializationContext
    {
        new ITextNodeProvider ScanNodeProvider { get; }

        INodeSealer<ITextNode> Sealer { get; }

        IStringPrimitiveScanner PrimitiveScanner { get; }
    }
}