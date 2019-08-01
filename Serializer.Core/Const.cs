using System;
using System.Reflection;

namespace Serializer
{
    internal static class Const
    {
        public static readonly Type StrType = typeof(String);
        public static readonly Type IConvertible = typeof(IConvertible);
        public static readonly Type ObjectType = typeof(Object);

        public const String TypeWrap = "__type";
        public const String Equal = "=";
        public const String Tsystem = "System";
        public const String WutXml = "?xml";
        public const String XmlType = "xsi:type";

        public const String StrQuote = "\"";
        public const String Root = "$";

        public const Char Space = ' ';
        public const Char Comma = ',';
        public const Char Dot = '.';
        public const Char Quote = '\"';
        public const Char SingleQuote = '\'';
        public const Char BackSlash = '\\';
        public const Char CarriageReturn = '\r';
        public const Char newLine = '\n';
        public const Char RootChar = '$';

        public const Int32 BACKSLASH = 92;

        public const BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
    }
}