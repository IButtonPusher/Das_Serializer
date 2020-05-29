using System;



namespace Das.Serializer
{
    public interface IStringPrimitiveScanner : IPrimitiveScanner<String>
    {
        String Descape(String input);
    }
}