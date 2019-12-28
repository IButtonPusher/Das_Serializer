using System;
using System.Reflection;

namespace Das.Serializer
{
    internal static class Const
    {
        public static readonly Type StrType = typeof(String);
        public static Type DbNull = typeof(DBNull);
        public static readonly Type IConvertible = typeof(IConvertible);
        public static readonly Type ObjectType = typeof(Object);

        public static readonly Type[] SingleObjectTypeArray = {ObjectType};
        public static readonly Type[] TwoObjectTypeArray = { ObjectType, ObjectType };

        public const String TypeWrap = "__type";
        public const String Equal = "=";
        public const String Tsystem = "System";
        public const String WutXml = "?xml";
        public const String XmlType = "xsi:type";
        public const String Empty = "";

        public const String StrQuote = "\"";
        public const String Root = "$";

        public const Char Space = ' ';
        public const Char Comma = ',';
        
        public const Char Quote = '\"';
        public const Char SingleQuote = '\'';
        public const Char BackSlash = '\\';
        public const Char CarriageReturn = '\r';

       public const Char OpenBrace = '{';
       public const Char CloseBrace = '}';
       public const Char OpenBracket = '[';
       public const Char CloseBracket = ']';

        public const Int32 BACKSLASH = 92;

        public const Int32 VarInt = 0;

        public const Int32 Int64 = 1;

        public static readonly Type IntType = typeof(Int32);
        public static readonly Type ByteType = typeof(Byte);
        public static readonly Type ByteArrayType = typeof(Byte[]);

        public const Int32 LengthDelimited = 2;

        public const BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
    }
}