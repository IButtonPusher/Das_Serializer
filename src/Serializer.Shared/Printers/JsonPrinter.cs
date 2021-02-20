using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Printers
{
    public class JsonPrinter : TextPrinter
    {
        private readonly ITypeManipulator _typeManipulator;

        public JsonPrinter(ITextRemunerable writer,
                           ISerializerSettings settings,
                           ITypeInferrer typeInferrer,
                           INodeTypeProvider nodeTypes,
                           IObjectManipulator objectManipulator,
                           ITypeManipulator typeManipulator)
            : base(writer, settings, typeInferrer, nodeTypes, objectManipulator)
        {
            _typeManipulator = typeManipulator;
        }

        public override Boolean IsRespectXmlIgnore => false;

        public static void AppendEscaped(String value,
                                         ITextRemunerable writer)
        {
            if (String.IsNullOrEmpty(value))
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


        public sealed override void PrintNode(String name,
                                              Type? propType,
                                              Object? val)
        {
            if (val == null)
                return;

            if (!_isIgnoreCircularDependencies)
            {
                if (!String.IsNullOrWhiteSpace(name))
                    PushStack(name);
                else if (propType?.FullName != null)
                    PushStack(propType.FullName);
                else
                    PushStack(Const.Root);
            }

            try
            {
                var isCloseBlock = false;
                var valType = val.GetType();
                var isWrapping = IsWrapNeeded(propType!, valType);

                var nodeType = _nodeTypes.GetNodeType(valType);

                if (!String.IsNullOrEmpty(name))
                {
                    if (name.Equals(PathAttribute))
                    {
                        Writer.Append(Const.OpenBrace);
                        TabOut();
                        isCloseBlock = true;
                    }

                    Writer.Append(Const.Quote, name);
                    Writer.Append("\":");
                }
                else if (!isWrapping)
                {
                    //var res = _nodeTypes.GetNodeType(valType);
                    //root node, we have to wrap primitives
                    if (nodeType == NodeTypes.Primitive || nodeType == NodeTypes.Fallback)
                    {
                        Writer.Append(Const.OpenBrace, _newLine);
                        TabOut();
                        isCloseBlock = true;
                    }
                }

                if (isWrapping)
                {
                    Writer.Append(Const.OpenBrace);
                    TabOut();
                    NewLine();
                    var clear = _typeInferrer.ToClearName(valType);

                    Writer.Append(Const.StrQuote, Const.TypeWrap);
                    Writer.Append(Const.StrQuote, ":");
                    Writer.Append(Const.StrQuote, clear);
                    Writer.Append(Const.StrQuote, ",");

                    NewLine();

                    Writer.Append(Const.Quote, Const.Val);
                    Writer.Append(Const.Quote, ": ");
                    TabIn();
                    isCloseBlock = true;
                }

                propType = valType;

                PrintObject(val, propType, nodeType);

                if (!isCloseBlock)
                    return;
                NewLine();

                Writer.Append(Const.CloseBrace);
            }
            finally
            {
                if (!_isIgnoreCircularDependencies)
                    PopStack();
            }
        }


        protected override void PrintCollection(Object? value,
                                                Type valType,
                                                Boolean knownEmpty)
        {
            if (ReferenceEquals(value, null))
                return;

            var serializeAs = value.GetType();

            if (!_isIgnoreCircularDependencies)
                PushStack($"[{serializeAs}]");

            Writer.Append(Const.OpenBracket);
            TabOut();

            var germane = _typeInferrer.GetGermaneType(serializeAs);

            PrintSeries(ExplodeIterator((value as IEnumerable)!, germane),
                PrintCollectionObject);


            TabIn();
            Writer.Append(Const.CloseBracket);
            if (!_isIgnoreCircularDependencies)
                PopStack();
        }

        protected override void PrintCollectionObject(Object? o,
                                                      Type propType,
                                                      Int32 index)
        {
            var nodeType = _nodeTypes.GetNodeType(propType);
            PrintObject(o, propType, nodeType);
        }

        protected override void PrintProperties(IEnumerable<KeyValuePair<PropertyInfo, object?>> values,
                                                Action<PropertyInfo, object?> exe)
        {
            using (var itar = values.GetEnumerator())
            {
                if (!itar.MoveNext())
                    return;

                var current = itar.Current;

                var printSep = ShouldPrintValue(current.Value);
                if (printSep)
                    exe(current.Key, current.Value);

                while (itar.MoveNext())
                {
                    current = itar.Current;

                    if (!ShouldPrintValue(current.Value))
                        continue;

                    if (printSep)
                        Writer.Append(SequenceSeparator);

                    exe(current.Key, current.Value);

                    printSep = true;
                }
            }
        }

        protected sealed override void PrintProperty(Object? propValue,
                                                     String name,
                                                     Type propertyType)
        {
            switch (_settings.PrintJsonPropertiesFormat)
            {
                case PrintPropertyFormat.Default:
                    break;

                case PrintPropertyFormat.PascalCase:
                    name = _typeInferrer.ToPascalCase(name);
                    break;

                case PrintPropertyFormat.CamelCase:
                    name = _typeInferrer.ToCamelCase(name);
                    break;

                case PrintPropertyFormat.SnakeCase:
                    name = _typeInferrer.ToSnakeCase(name);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.PrintProperty(propValue, name, propertyType);
        }

        protected sealed override void PrintReferenceType(Object? value,
                                                          Type valType)
        {
            if (value == null)
            {
                Writer.Append(DasCoreSerializer.StrNull);
                return;
            }

            var wasAny = !Writer.IsEmpty;

            if (wasAny)
            {
                TabOut();
                NewLine();

                Writer.Append(Const.OpenBrace);

                TabOut();
                NewLine();
            }
            else
                Writer.Append(Const.OpenBrace);


            var typeStruct = _typeManipulator.GetTypeStructure(valType, _settings);
            var properties = typeStruct.Properties;
            if (properties.Length > 0)
            {
                var printSep = false;

                for (var c = 0; c < properties.Length; c++)
                {
                    var prop = properties[c];

                    var currentValue = prop.GetPropertyValue(value);
                    if (!ShouldPrintValue(currentValue))
                    {
                        continue;
                    }

                    if (printSep)
                        Writer.Append(SequenceSeparator);


                    PrintProperty(currentValue, prop.PropertyPath, prop.PropertyType);
                    printSep = true;
                }
            }
            

            if (wasAny)
            {
                TabIn();
                NewLine();
                Writer.Append(Const.CloseBrace);
                TabIn();
                NewLine();
            }
            else
                Writer.Append(Const.CloseBrace);
        }

        protected override void PrintSeries(IEnumerable<KeyValuePair<object?, Type>> values,
                                            Action<object?, Type, Int32> print)
        {
            var idx = 0;

            using (var itar = values.GetEnumerator())
            {
                if (!itar.MoveNext())
                    return;

                var printSep = ShouldPrintValue(itar.Current);
                if (printSep)
                    print(itar.Current.Key, itar.Current.Value, idx++);


                while (itar.MoveNext())
                {
                    var current = itar.Current;

                    if (!ShouldPrintValue(current))
                        continue;

                    if (printSep)
                        Writer.Append(SequenceSeparator);


                    print(itar.Current.Key, itar.Current.Value, idx++);
                    printSep = true;
                }
            }
        }

        [MethodImpl(256)]
        protected sealed override void PrintString(String str)
        {
            Writer.Append(Const.Quote);
            AppendEscaped(str, Writer);
            Writer.Append(Const.Quote);
        }

        [MethodImpl(256)]
        protected sealed override void PrintStringWithoutEscaping(String str)
        {
            Writer.Append(Const.Quote);
            Writer.Append(str);
            Writer.Append(Const.Quote);
        }

        [MethodImpl(256)]
        protected sealed override void PrintChar(Char c)
        {
            Writer.Append(Const.Quote);
            Writer.Append(c);
            Writer.Append(Const.Quote);
        }

        [MethodImpl(256)]
        protected sealed override void PrintString(String str,
                                                   Boolean isInQuotes)
        {
            if (isInQuotes)
                Writer.Append(Const.Quote);

            AppendEscaped(str, Writer);

            if (isInQuotes)
                Writer.Append(Const.Quote);
        }

        [MethodImpl(256)]
        protected sealed override void PrintInteger(Object val)
        {
            Writer.Append(val.ToString());
        }

        [MethodImpl(256)]
        protected sealed override void PrintReal(String str)
        {
            Writer.Append(str);
        }


        private const Char SequenceSeparator = ',';
    }
}
