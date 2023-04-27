using System;
using System.Threading.Tasks;

namespace Das.Serializer;

/// <summary>
///     Stateless facade for binary deserialization
/// </summary>
public interface IBinaryContext : ISerializationCore
{
   IBinaryPrimitiveScanner PrimitiveScanner { get; }

   IBinaryNodeProvider ScanNodeProvider { get; }
}