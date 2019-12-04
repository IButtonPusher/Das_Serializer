namespace Das.Serializer
{
    /// <summary>
    /// Stateless facade for binary deserialization
    /// </summary>
    public interface IBinaryContext : ISerializationContext
    {
        IBinaryNodeProvider NodeProvider { get; }

        IBinaryPrimitiveScanner PrimitiveScanner { get; }

        BinaryLogger Logger { get; }
    }
}