using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Printers
{
    public class JsonPrinter : TextPrinter
    {
        public JsonPrinter(ITypeInferrer typeInferrer,
                           INodeTypeProvider nodeTypes,
                           IObjectManipulator objectManipulator,
                           ITypeManipulator typeManipulator)
            : base(typeInferrer, nodeTypes, objectManipulator, '.',
                   typeManipulator)
        {
            
        }

        //public static void AppendDateTime<TWriter>(TWriter writer,
        //                                           DateTime value)
        //    where TWriter : ITextRemunerable
        //{
        //    writer.Append(value.Year.ToString());
        //    writer.Append('-');
        //}


        public static void AppendEscaped<TWriter>(TWriter writer,
                                                  String value)
            where TWriter : ITextRemunerable
        {
            if (ReferenceEquals(null, value))
                return;

            var len = value.Length;
            var needEncode = false;
            Char cIn;
            for (var i = 0; i < len; i++)
            {
                cIn = value[i];

                if ((cIn < 0 || cIn > 31) && cIn != 34 && cIn != 39 && cIn != 60 &&
                    cIn != 62 && cIn != 92)
                    continue;
                needEncode = true;
                break;
            }

            if (!needEncode)
            {
                writer.Append(value);
                return;
            }

            for (var i = 0; i < len; i++)
            {
                cIn = value[i];

                if (cIn >= 0 && cIn <= 7 || cIn == 11 || cIn >= 14 && cIn <= 31
                    || cIn == 39 || cIn == 60 || cIn == 62)
                {
                    writer.Append($"\\u{(Int32) cIn:x4}");
                    continue;
                }

                String cOut;
                // ReSharper disable once RedundantCast
                switch ((Int32) cIn)
                {
                    case 8:
                        cOut = "\\b";
                        break;
                    case 9:
                        cOut = "\\t";
                        break;
                    case 10:
                        cOut = "\\n";
                        break;
                    case 12:
                        cOut = "\\f";
                        break;
                    case 13:
                        cOut = "\\r";
                        break;
                    case 34:
                        cOut = "\\\"";
                        break;
                    case 92:
                        cOut = "\\\\";
                        break;

                    default:
                        writer.Append(cIn);
                        continue;
                }

                writer.Append(cOut);
            }
        }


        private static String GetNameOrTypeNameOrRoot(String name,
                                                      Type? propType)
        {
            if (!String.IsNullOrWhiteSpace(name))
                return name;
            if (propType?.FullName != null)
                return propType.FullName;

            return Const.Root;
        }

        public sealed override void PrintNamedObject(String name,
                                                     Type? propType,
                                                     Object? val,
                                                     NodeTypes valueNodeType,
                                                     ITextRemunerable Writer,
                                                     ISerializerSettings settings,
                                                     ICircularReferenceHandler circularReferenceHandler)
        {
            if (val == null)
                return;

            circularReferenceHandler.AddPathReference(name, propType, GetNameOrTypeNameOrRoot);

            try
            {
                var isCloseBlock = false;
                var valType = val.GetType();
                
                var isWrapping = IsWrapNeeded(propType!, valType, 
                    valueNodeType, settings);

                if (!String.IsNullOrEmpty(name))
                {
                    if (name.Equals(PathAttribute))
                    {
                        Writer.Append(Const.OpenBrace);
                        Writer.TabOut();
                        isCloseBlock = true;
                    }

                    Writer.Append(Const.Quote, name);
                    Writer.Append("\":");
                }
                

                if (isWrapping)
                {
                    Writer.Append(Const.OpenBrace);
                    Writer.TabOut();
                    Writer.NewLine();
                    var clear = _typeInferrer.ToClearName(valType);

                    Writer.Append(Const.StrQuote, Const.TypeWrap);
                    Writer.Append(Const.StrQuote, ":");
                    Writer.Append(Const.StrQuote, clear);
                    Writer.Append(Const.StrQuote, ",");

                    Writer.NewLine();

                    Writer.Append(Const.Quote, Const.Val);
                    Writer.Append(Const.Quote, ": ");
                    Writer.TabIn();
                    isCloseBlock = true;
                }

                propType = valType;


                //////////////////////////////
                PrintObject(val, propType, valueNodeType, Writer, settings, circularReferenceHandler);
                //////////////////////////////

                if (!isCloseBlock)
                    return;
                Writer.NewLine();

                Writer.Append(Const.CloseBrace);
            }
            finally
            {
                circularReferenceHandler.PopPathReference();
            }
        }

        public sealed override void PrintReferenceType(Object? value,
                                                       Type valType,
                                                       NodeTypes nodeType,
                                                       ITextRemunerable Writer,
                                                       ISerializerSettings settings,
                                                       ICircularReferenceHandler circularReferenceHandler)
        {
            if (value == null)
            {
                Writer.Append(DasCoreSerializer.StrNull);
                return;
            }

            var wasAny = !Writer.IsEmpty;

            if (wasAny)
            {
                Writer.TabOut();
                Writer.NewLine();

                Writer.Append(Const.OpenBrace);

                Writer.TabOut();
                Writer.NewLine();
            }
            else
                Writer.Append(Const.OpenBrace);


            var typeStruct = _typeManipulator.GetTypeStructure(valType);
            var properties = typeStruct.Properties;
            if (properties.Length > 0)
            {
                var printSep = false;

                for (var c = 0; c < properties.Length; c++)
                {
                    var prop = properties[c];

                    var currentValue = prop.GetPropertyValue(value);
                    if (!ShouldPrintValue(currentValue, settings))
                        continue;

                    var valueNodeType = _nodeTypes.GetNodeType(currentValue?.GetType() ?? prop.PropertyType);

                    if (printSep)
                        Writer.Append(SequenceSeparator);

                    var propName = prop.PropertyPath;

                    switch (settings.PrintPropertyNameFormat)
                    {
                        case PropertyNameFormat.Default:
                            break;

                        case PropertyNameFormat.PascalCase:
                            propName = _typeInferrer.ToPascalCase(propName);
                            break;

                        case PropertyNameFormat.CamelCase:
                            propName = _typeInferrer.ToCamelCase(propName);
                            break;

                        case PropertyNameFormat.SnakeCase:
                            propName = _typeInferrer.ToSnakeCase(propName);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (valueNodeType != NodeTypes.Collection)
                        PrintNamedObject(propName, prop.PropertyType, currentValue, 
                            valueNodeType, Writer, settings, circularReferenceHandler);
                    else
                        PrintProperty(currentValue, propName, prop.PropertyType, 
                            Writer, valueNodeType, settings, circularReferenceHandler);
                    printSep = true;
                }
            }


            if (wasAny)
            {
                Writer.TabIn();
                Writer.NewLine();
                Writer.Append(Const.CloseBrace);
                Writer.TabIn();
                Writer.NewLine();
            }
            else
                Writer.Append(Const.CloseBrace);
        }

        protected sealed override bool ShouldPrintValue(Object obj,
                                                        NodeTypes nodeType,
                                                 IPropertyAccessor prop,
                                                 ISerializerSettings settings,
                                                 out Object? value)
        {
            value = prop.GetPropertyValue(obj);
            return ShouldPrintValue(value, settings);
        }

        protected bool ShouldPrintValue<T>(T item,
                                           ISerializerSettings settings)
        {
            return !ReferenceEquals(null, item) ||
                   (!settings.IsOmitDefaultValues ||
                    !_typeInferrer.IsDefaultValue(item));
        }

        [MethodImpl(256)]
        protected sealed override void PrintChar(ITextRemunerable Writer,
                                                 Char c)
        {
            Writer.Append(Const.Quote);
            Writer.Append(c);
            Writer.Append(Const.Quote);
        }

        private static String GetCollectionReferenceText(Type serializeAs)
            => $"[{serializeAs}]";


        protected sealed override void PrintCollection(Object? value,
                                                       Type valType,
                                                       ITextRemunerable Writer,
                                                       ISerializerSettings settings,
                                                       ICircularReferenceHandler circularReferenceHandler)
        {
            if (ReferenceEquals(value, null))
                return;

            var serializeAs = value.GetType();
            circularReferenceHandler.AddPathReference(serializeAs, GetCollectionReferenceText);


            Writer.Append(Const.OpenBracket);
            Writer.TabOut();

            var germane = _typeInferrer.GetGermaneType(serializeAs);
            var germaneNodeType = _nodeTypes.GetNodeType(germane);

            switch (value)
            {
                case IList list:
                    if (list.Count == 0)
                        break;

                    PrintObject(list[0], germane, germaneNodeType, Writer, settings, circularReferenceHandler);

                    for (var c = 1; c < list.Count; c++)
                    {
                        Writer.Append(SequenceSeparator);
                        PrintObject(list[c], germane, germaneNodeType, Writer,settings, 
                            circularReferenceHandler);
                    }

                    break;

                case IEnumerable ienum:

                    var printSep = false;

                    foreach (var o in ienum)
                    {
                        if (printSep)
                            Writer.Append(SequenceSeparator);
                        printSep = true;
                        PrintObject(o, germane, germaneNodeType, Writer,
                            settings, circularReferenceHandler);
                    }

                    break;
            }

            Writer.TabIn();
            Writer.Append(Const.CloseBracket);
            circularReferenceHandler.PopPathReference();
        }


        [MethodImpl(256)]
        protected sealed override void PrintInteger(Object val,
                                                    ITextRemunerable Writer)
        {
            Writer.Append(val.ToString());
        }


        [MethodImpl(256)]
        protected sealed override void PrintReal(String str,
                                                 ITextRemunerable Writer)
        {
            Writer.Append(str);
        }

        [MethodImpl(256)]
        protected sealed override void PrintString(String str,
                                                   ITextRemunerable Writer)
        {
            Writer.Append(Const.Quote);
            AppendEscaped(Writer, str);
            Writer.Append(Const.Quote);
        }

        [MethodImpl(256)]
        protected sealed override void PrintString(String str,
                                                   Boolean isInQuotes,
                                                   ITextRemunerable Writer)
        {
            if (isInQuotes)
                Writer.Append(Const.Quote);

            AppendEscaped(Writer, str);

            if (isInQuotes)
                Writer.Append(Const.Quote);
        }

        [MethodImpl(256)]
        protected sealed override void PrintStringWithoutEscaping(String str,
                                                                  ITextRemunerable Writer)
        {
            Writer.Append(Const.Quote);
            Writer.Append(str);
            Writer.Append(Const.Quote);
        }


        private const Char SequenceSeparator = ',';
    }
}
