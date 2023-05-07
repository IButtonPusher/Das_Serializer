using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Das.Serializer;

public static class Const
{
   public const String TypeWrap = "__type";
   public const String Val = "__val";
   public const String Equal = "=";
   public const String Tsystem = "System";
   public const String WutXml = "?xml";
   public const String XmlType = "xsi:type";
   public const String XmlXsiAttribute = "xmlns:xsi";
   public const String XmlNsXsd = "xmlns:xsd";
   public const String XmlNs = "xmlns";
   public const String XmlNsLink = "xmlns:xlink";

   public const String XmlXsiNamespace = " " + XmlXsiAttribute + "=\"http://www.w3.org/2001/XMLSchema-instance\"";

   //public const String XmlXsiNamespace = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"";
   public const String XmlNull = "xsi:nil";
   public const String Empty = "";

   public const String RefAttr = "$ref";
   public const String RefTag = "__ref";

   public const String StrQuote = "\"";
   public const String Root = "$";

   public const Char Space = ' ';
   public const Char Comma = ',';

   public const Char Quote = '\"';
   public const Char SingleQuote = '\'';
   public const Char BackSlash = '\\';
   public const Char CarriageReturn = '\r';
   public const Char NewLine = '\n';
   public const Char Tab = '\t';

   public const Char OpenBrace = '{';
   public const Char CloseBrace = '}';
   public const Char OpenBracket = '[';
   public const Char CloseBracket = ']';

   public const Int32 BACKSLASH = 92;

   public const Int32 VarInt = 0;

   public const Int32 Int64 = 1;

   public const Int32 LengthDelimited = 2;

   public const BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

   public const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

   public const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;

   public const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

   public const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

   public const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

   public static readonly List<Type> EmptyTypeList = new(0);
   public static readonly Type StrType = typeof(String);
   public static Type DbNull = typeof(DBNull);
   public static readonly Type IConvertible = typeof(IConvertible);
   public static readonly Type ObjectType = typeof(Object);

   public static readonly Type[] SingleObjectTypeArray = {ObjectType};
   public static readonly Type[] TwoObjectTypeArray = {ObjectType, ObjectType};

   public static readonly Type IntType = typeof(Int32);
   public static readonly Type ByteType = typeof(Byte);
   public static readonly Type ByteArrayType = typeof(Byte[]);

   public static readonly Object[] EmptyObjectList =
      #if NET40
            new Object[0];
      #else
      Array.Empty<Object>();
   #endif
}