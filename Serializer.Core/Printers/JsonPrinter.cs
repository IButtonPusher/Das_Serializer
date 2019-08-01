﻿using Das.Remunerators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Das.Serializer;
using Das.Serializer.Objects;
using Serializer;
using Serializer.Core.Printers;

namespace Das.Printers
{
    internal class JsonPrinter : TextPrinter
    {
        private readonly ISerializationState _stateProvider;

        #region construction

        private const Char OpenBrace = '{';
        private const Char CloseBrace = '}';
        private const Char OpenCollection = '[';
        private const Char CloseCollection = ']';

        public JsonPrinter(ITextRemunerable writer, ISerializationState stateProvider)
            : base(writer, stateProvider)
        {
            _stateProvider = stateProvider;
            SequenceSeparator = ',';
        }

        #endregion

        #region public interface

        public override Boolean PrintNode(NamedValueNode node)
        {
            var name = node.Name;
            var propType = node.Type;
            var val = node.Value;

            if (val == null)
                return false;

            if (!_isIgnoreCircularDependencies)
                PushStack(String.IsNullOrWhiteSpace(name) ? Const.Root : name);

            try
            {
                var isCloseBlock = false;
                var valType = val.GetType();
                var isWrapping = IsWrapNeeded(propType, valType);

                if (!String.IsNullOrWhiteSpace(name))
                {
                    if (name.Equals(PathAttribute))
                    {
                        Writer.Append(OpenBrace);
                        TabOut();
                        isCloseBlock = true;
                    }

                    Writer.Append(Const.Quote, name);
                    Writer.Append(Const.Quote, ":");
                }
                else if (!isWrapping)
                {
                    var res = _stateProvider.GetNodeType(valType, Settings.SerializationDepth);
                    //root node, we have to wrap primitives
                    if (res == NodeTypes.Primitive || res == NodeTypes.Fallback)
                    {
                        Writer.Append(OpenBrace, _newLine);
                        TabOut();
                        isCloseBlock = true;
                    }
                }

                if (isWrapping)
                {
                    Writer.Append(OpenBrace);
                    TabOut();
                    NewLine();
                    var clear = _stateProvider.TypeInferrer.ToClearName(valType, false);

                    Writer.Append(Const.StrQuote, Const.TypeWrap);
                    Writer.Append(Const.StrQuote, ":");
                    Writer.Append(Const.StrQuote, clear);
                    Writer.Append(Const.StrQuote, ",");

                    NewLine();

                    Writer.Append(Const.Quote, DasCoreSerializer.Val);
                    Writer.Append(Const.Quote, ": ");
                    TabIn();
                    isCloseBlock = true;
                }

                ///////////////////
                var nodeType = _stateProvider.GetNodeType(valType, Settings.SerializationDepth);
                var print = new PrintNode(node, nodeType);
                PrintObject(print);
                //////////////////

                if (!isCloseBlock)
                    return true;
                NewLine();

                Writer.Append(CloseBrace);

                return true;
            }
            finally
            {
                if (!_isIgnoreCircularDependencies)
                    PopStack();
            }
        }

        protected override void PrintSeries<T>(IEnumerable<T> values, Func<T, Boolean> meth)
        {
            using (var itar = values.GetEnumerator())
            {
                if (!itar.MoveNext())
                    return;

                var printSep = meth(itar.Current);

                while (itar.MoveNext())
                {
                    if (printSep)
                        Writer.Append(SequenceSeparator);

                    printSep = meth(itar.Current);
                }
            }
        }

        #endregion

        #region private implementation primary

        protected override void PrintReferenceType(PrintNode node)
        {
            if (node.Value == null)
            {
                Writer.Append(DasCoreSerializer.StrNull);
                return;
            }

            var wasAny = !Writer.IsEmpty;

            if (wasAny)
            {
                TabOut();
                NewLine();

                Writer.Append(OpenBrace);

                TabOut();
                NewLine();
            }
            else Writer.Append(OpenBrace);

            base.PrintReferenceType(node);

            if (wasAny)
            {
                TabIn();
                NewLine();
                Writer.Append(CloseBrace);
                TabIn();
                NewLine();
            }
            else Writer.Append(CloseBrace);
        }

        private void PrintSpecialDictionary(IDictionary dic)
        {
            Writer.Append(OpenBrace);
            TabOut();

            var enumerator = dic.GetEnumerator();
            if (!enumerator.MoveNext())
                return;

            var kvp = enumerator.Entry;

            PrintNode(kvp);

            while (enumerator.MoveNext())
            {
                kvp = enumerator.Entry;
                Writer.Append(SequenceSeparator);
                PrintNode(kvp);
            }

            TabIn();
            Writer.Append(CloseBrace);
        }

        protected override void PrintCollection(PrintNode node)
        {
            var serializeAs = node.Value.GetType();

            if (!_isIgnoreCircularDependencies)
                PushStack($"[{serializeAs}]");
            if (typeof(IDictionary).IsAssignableFrom(serializeAs) &&
                serializeAs.GetGenericArguments().FirstOrDefault() == typeof(string)
                && node.Value is IDictionary dic)
            {
                PrintSpecialDictionary(dic);
                return;
            }

            Writer.Append(OpenCollection);
            TabOut();

            var germane = _stateProvider.TypeInferrer.GetGermaneType(serializeAs);

            PrintSeries(ExplodeList(node.Value as IEnumerable, germane),
                PrintCollectionObject);

            TabIn();
            Writer.Append(CloseCollection);
            if (!_isIgnoreCircularDependencies)
                PopStack();
        }

        protected Boolean PrintCollectionObject(ObjectNode val)
        {
            var res = _stateProvider.GetNodeType(val.Type, Settings.SerializationDepth);
            var print = new PrintNode(val, res);
            PrintObject(print);

            return true;
        }

        protected override void PrintFallback(PrintNode node)
        {
            Writer.Append(Const.Quote);
            node.Type = node.Value.GetType();

            PrintPrimitive(node);
            Writer.Append(Const.Quote);
        }

        #endregion

        #region private implementation helpers

        protected override void PrintString(string str, Boolean isInQuotes)
        {
            if (isInQuotes)
                Writer.Append(Const.Quote);

            AppendEscaped(str);

            if (isInQuotes)
                Writer.Append(Const.Quote);
        }

        private void AppendEscaped(String value)
        {
            if (String.IsNullOrEmpty(value))
                return;

            var len = value.Length;
            var needEncode = false;
            char cIn;
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
                Writer.Append(value);
                return;
            }

            for (var i = 0; i < len; i++)
            {
                cIn = value[i];

                if (cIn >= 0 && cIn <= 7 || cIn == 11 || cIn >= 14 && cIn <= 31
                    || cIn == 39 || cIn == 60 || cIn == 62)
                {
                    Writer.Append($"\\u{(Int32) cIn:x4}");
                    continue;
                }

                String cOut;
                switch ((int) cIn)
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
                        Writer.Append(cIn);
                        continue;
                }

                Writer.Append(cOut);
            }
        }

        #endregion
    }
}