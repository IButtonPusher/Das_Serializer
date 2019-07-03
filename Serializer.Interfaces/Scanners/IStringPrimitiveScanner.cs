using System;
using Das.Scanners;

namespace Das.Serializer
{
    public interface IStringPrimitiveScanner : IPrimitiveScanner<String>
    {
        String Descape(String input);
    }
}
