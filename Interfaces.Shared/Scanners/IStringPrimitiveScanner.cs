using System;
using Das.Serializer.Scanners;


namespace Das.Serializer
{
    public interface IStringPrimitiveScanner : IPrimitiveScanner<String>
    {
        String Descape(String input);
    }
}