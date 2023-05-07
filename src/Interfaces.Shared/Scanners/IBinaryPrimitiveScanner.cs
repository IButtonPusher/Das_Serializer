using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IBinaryPrimitiveScanner : IPrimitiveScanner<Byte[]>
{
   Int32 GetInt32(Byte[] arr);

   String? GetString(Byte[] tempByte);

   T GetValue<T>(Byte[] input);
}