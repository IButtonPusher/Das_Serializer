using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    /// <summary>
    ///     Stateless facade for binary deserialization
    /// </summary>
    public interface IBinaryContext : ISerializationContext
    {
        IBinaryPrimitiveScanner PrimitiveScanner { get; }

        new IBinaryNodeProvider ScanNodeProvider { get; }
    }
}
