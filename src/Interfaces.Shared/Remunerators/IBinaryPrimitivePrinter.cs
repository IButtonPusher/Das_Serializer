using System;

namespace Das.Serializer.Remunerators;

public interface IBinaryPrimitivePrinter
{
   void PrintPrimitive<T>(T o,
                          IBinaryWriter bWriter);
}
