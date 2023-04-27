using System;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IStringPrimitiveScanner : IPrimitiveScanner<String>
{
   String Descape(String input);
}