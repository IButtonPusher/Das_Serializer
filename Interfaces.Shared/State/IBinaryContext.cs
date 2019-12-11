namespace Das.Serializer
{
    /// <summary>
    /// Stateless facade for binary deserialization
    /// </summary>
    public interface IBinaryContext : ISerializationContext
    {
        new IBinaryNodeProvider ScanNodeProvider { get; }

        IBinaryPrimitiveScanner PrimitiveScanner { get; }

        BinaryLogger Logger { get; }
    }
}