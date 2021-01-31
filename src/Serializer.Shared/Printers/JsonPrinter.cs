using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Printers
{
    public class JsonPrinter : TextPrinter
    {
        public JsonPrinter(ITextRemunerable writer, 
                           ISerializerSettings settings,
                           ITypeInferrer typeInferrer,
                           INodeTypeProvider nodeTypes,
                           IObjectManipulator objectManipulator)
                           //ISerializationState stateProvider)
            : base(writer, //stateProvider, 
                settings, typeInferrer, nodeTypes, objectManipulator)
        {
          //  _stateProvider = stateProvider;
        }


        private const Char SequenceSeparator = ',';
        //private readonly ISerializationState _stateProvider;

        

        //public override void PrintNode(INamedValue node)
        //{
        //    var name = node.Name;
        //    var propType = node.Type;
        //    var val = node.Value;

        //    if (val == null)
        //        return;

        //    if (!_isIgnoreCircularDependencies)
        //    {
        //        if (!String.IsNullOrWhiteSpace(name))
        //            PushStack(name);
        //        else if (node.Type?.FullName != null)
        //            PushStack(node.Type.FullName);
        //        else
        //            PushStack(Const.Root);
        //        //PushStack(String.IsNullOrWhiteSpace(name) ? Const.Root : name);
        //    }

        //    try
        //    {
        //        var isCloseBlock = false;
        //        var valType = val.GetType();
        //        var isWrapping = IsWrapNeeded(propType!, valType);

        //        if (!String.IsNullOrWhiteSpace(name))
        //        {
        //            if (name.Equals(PathAttribute))
        //            {
        //                Writer.Append(Const.OpenBrace);
        //                TabOut();
        //                isCloseBlock = true;
        //            }

        //            Writer.Append(Const.Quote, name);
        //            Writer.Append(Const.Quote, ":");
        //        }
        //        else if (!isWrapping)
        //        {
        //            var res = _nodeTypes.GetNodeType(valType, Settings.SerializationDepth);
        //            //root node, we have to wrap primitives
        //            if (res == NodeTypes.Primitive || res == NodeTypes.Fallback)
        //            {
        //                Writer.Append(Const.OpenBrace, _newLine);
        //                TabOut();
        //                isCloseBlock = true;
        //            }
        //        }

        //        if (isWrapping)
        //        {
        //            Writer.Append(Const.OpenBrace);
        //            TabOut();
        //            NewLine();
        //            var clear = _typeInferrer.ToClearName(valType, false);

        //            Writer.Append(Const.StrQuote, Const.TypeWrap);
        //            Writer.Append(Const.StrQuote, ":");
        //            Writer.Append(Const.StrQuote, clear);
        //            Writer.Append(Const.StrQuote, ",");

        //            NewLine();

        //            Writer.Append(Const.Quote, Const.Val);
        //            Writer.Append(Const.Quote, ": ");
        //            TabIn();
        //            isCloseBlock = true;
        //        }

        //        node.Type = valType;

        //        using (var print = _printNodePool.GetPrintNode(node))
        //        {
        //            PrintObject(print);
        //        }

        //        if (!isCloseBlock)
        //            return;
        //        NewLine();

        //        Writer.Append(Const.CloseBrace);
        //    }
        //    finally
        //    {
        //        if (!_isIgnoreCircularDependencies)
        //            PopStack();
        //    }
        //}

        public override void PrintNode(String name,
                                       Type? propType,
                                       Object? val)
            //NodeTypes nodeType)
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
                //PushStack(String.IsNullOrWhiteSpace(name) ? Const.Root : name);
            }

            try
            {
                var isCloseBlock = false;
                var valType = val.GetType();
                var isWrapping = IsWrapNeeded(propType!, valType);

                if (!String.IsNullOrWhiteSpace(name))
                {
                    if (name.Equals(PathAttribute))
                    {
                        Writer.Append(Const.OpenBrace);
                        TabOut();
                        isCloseBlock = true;
                    }

                    Writer.Append(Const.Quote, name);
                    Writer.Append(Const.Quote, ":");
                }
                else if (!isWrapping)
                {
                    var res = _nodeTypes.GetNodeType(valType);
                    //root node, we have to wrap primitives
                    if (res == NodeTypes.Primitive || res == NodeTypes.Fallback)
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
                    var clear = _typeInferrer.ToClearName(valType, false);

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
                var nodeType = _nodeTypes.GetNodeType(propType);
                PrintObject(val, propType, nodeType);
                //using (var print = _printNodePool.GetPrintNode(node))
                //{
                //    PrintObject(print);
                //}

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


        //protected override void PrintSeries<T>(IEnumerable<T> values,
        //                                       Action<T> print)
        //{
        //    using (var itar = values.GetEnumerator())
        //    {
        //        if (!itar.MoveNext())
        //            return;

        //        var printSep = ShouldPrintValue(itar.Current);
        //        if (printSep)
        //            print(itar.Current);


        //        while (itar.MoveNext())
        //        {
        //            var current = itar.Current;

        //            if (!ShouldPrintValue(current))
        //                continue;

        //            if (printSep)
        //                Writer.Append(SequenceSeparator);


        //            print(current);
        //            printSep = true;
        //        }
        //    }
        //}

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

        //protected override void PrintProperties<T>(IPropertyValueIterator<T> values,
        //                                           Action<T> exe)
        //{
        //    var cnt = values.Count;
        //    if (cnt == 0)
        //        return;

        //    var current = values[0];

        //    var printSep = ShouldPrintNode(current);
        //    if (printSep)
        //        exe(current);

        //    for (var c = 1; c < values.Count; c++)
        //    {
        //        current = values[c];

        //        if (!ShouldPrintNode(current))
        //            continue;

        //        if (printSep)
        //            Writer.Append(SequenceSeparator);

        //        exe(current);

        //        printSep = true;
        //    }
        //}

        protected override void PrintProperty(Object? propValue,
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


        //protected override void PrintReferenceType(IPrintNode node)
        //{
        //    if (node.Value == null)
        //    {
        //        Writer.Append(DasCoreSerializer.StrNull);
        //        return;
        //    }

        //    var wasAny = !Writer.IsEmpty;

        //    if (wasAny)
        //    {
        //        TabOut();
        //        NewLine();

        //        Writer.Append(Const.OpenBrace);

        //        TabOut();
        //        NewLine();
        //    }
        //    else
        //        Writer.Append(Const.OpenBrace);

        //    base.PrintReferenceType(node);

        //    if (wasAny)
        //    {
        //        TabIn();
        //        NewLine();
        //        Writer.Append(Const.CloseBrace);
        //        TabIn();
        //        NewLine();
        //    }
        //    else
        //        Writer.Append(Const.CloseBrace);
        //}

        protected override void PrintReferenceType(Object? value,
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

            base.PrintReferenceType(value, valType);

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

        //private void PrintSpecialDictionary(IDictionary dic)
        //{
        //    Writer.Append(Const.OpenBrace);
        //    TabOut();

        //    var enumerator = dic.GetEnumerator();
        //    if (!enumerator.MoveNext())
        //        return;

        //    using (var kvp = _printNodePool.GetNamedValue(enumerator.Entry))
        //    {
        //        PrintNode(kvp);
        //    }

        //    while (enumerator.MoveNext())
        //        using (var kvp = _printNodePool.GetNamedValue(enumerator.Entry))
        //        {
        //            Writer.Append(SequenceSeparator);
        //            PrintNode(kvp);
        //        }

        //    TabIn();
        //    Writer.Append(Const.CloseBrace);
        //}

        //protected override void PrintCollection(IPrintNode node)
        //{
        //    var serializeAs = node.Value!.GetType();

        //    if (!_isIgnoreCircularDependencies)
        //        PushStack($"[{serializeAs}]");
        //    //if (typeof(IDictionary).IsAssignableFrom(serializeAs) &&
        //    //    serializeAs.GetGenericArguments().FirstOrDefault() == typeof(String)
        //    //    && node.Value is IDictionary dic)
        //    //{
        //    //    PrintSpecialDictionary(dic);
        //    //    return;
        //    //}

        //    Writer.Append(Const.OpenBracket);
        //    TabOut();

        //    var germane = _typeInferrer.GetGermaneType(serializeAs);

        //    PrintSeries(ExplodeList((node.Value as IEnumerable)!, germane),
        //        PrintCollectionObject);

        //    TabIn();
        //    Writer.Append(Const.CloseBracket);
        //    if (!_isIgnoreCircularDependencies)
        //        PopStack();
        //}

        protected override void PrintCollection(Object? value,
                                                Type valType,
                                                Boolean knownEmpty)
        {
            if (ReferenceEquals(value, null))
                return;

            var serializeAs = value.GetType();

            if (!_isIgnoreCircularDependencies)
                PushStack($"[{serializeAs}]");
            //if (typeof(IDictionary).IsAssignableFrom(serializeAs) &&
            //    serializeAs.GetGenericArguments().FirstOrDefault() == typeof(String)
            //    && value is IDictionary dic)
            //{
            //    PrintSpecialDictionary(dic);
            //    return;
            //}

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
            //base.PrintCollectionObject(o, propType, index);
        }

        //protected void PrintCollectionObject(ObjectNode val)
        //{
        //    using (var print = _printNodePool.GetPrintNode(val))
        //    {
        //        PrintObject(print);
        //    }
        //}

        //protected override void PrintFallback(IPrintNode node)
        //{
        //    Writer.Append(Const.Quote);
        //    node.Type = node.Value!.GetType();

        //    PrintPrimitive(node);
        //    Writer.Append(Const.Quote);
        //}

        protected override void PrintFallback(Object? o,
                                              Type propType)
        {
            Writer.Append(Const.Quote);

            PrintPrimitive(o, o!.GetType());
            Writer.Append(Const.Quote);
        }

        public override Boolean IsRespectXmlIgnore => false;

        

        

        protected override void PrintString(String str,
                                            Boolean isInQuotes)
        {
            if (isInQuotes)
                Writer.Append(Const.Quote);

            AppendEscaped(str, Writer);

            if (isInQuotes)
                Writer.Append(Const.Quote);
        }

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
    }
}
