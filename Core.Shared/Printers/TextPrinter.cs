using System;
using System.Collections.Generic;
using System.Text;
using Das.Serializer;

namespace Das.Printers
{
    internal abstract class TextPrinter : PrinterBase<Char>
    {
        protected TextPrinter(ITextRemunerable writer, ISerializationState stateProvider,
            ISerializerSettings settings) 
            : base(stateProvider, settings)
        {
            writer.Undispose();
            Writer = writer;
            _stateProvider = stateProvider;
            _tabs = new StringBuilder();
            _formatStack = new Stack<StackFormat>();
            _indenter = stateProvider.Settings.Indentation;
            _newLine = stateProvider.Settings.NewLine;
            _indentLength = _indenter.Length;
        }

        protected TextPrinter(ITextRemunerable writer, ISerializationState stateProvider)
            : this(writer, stateProvider, stateProvider.Settings)
        {
        }

        protected String Tabs => _tabs.ToString();

        private readonly StringBuilder _tabs;
        protected readonly Stack<StackFormat> _formatStack;

        protected readonly ITextRemunerable Writer;
        private readonly ISerializationState _stateProvider;

        protected readonly String _indenter;
        protected readonly String _newLine;
        private readonly Int32 _indentLength;


        protected static Boolean IsRequiresQuotes(Object o)
        {
            var oType = o?.GetType();
            if (oType == null)
                return false;
            return oType == Const.StrType || oType == typeof(DateTime) || oType.IsEnum;
        }

        protected void TabOut() => _tabs.Append(_indenter);

        protected void TabIn() => _tabs.Remove(0, _indentLength);

        protected void NewLine() => Writer.Append(_newLine + Tabs);


        protected override void PrintFallback(IPrintNode node)
        {
            node.Type = node.Value.GetType();
            PrintPrimitive(node);
        }

        /// <summary>
        /// xml puts all primitives as attributes and in quotes. Json does not put
        /// numeric types in quotes
        /// </summary>
        protected override void PrintPrimitive(IPrintNode node)
        {
            var o = node.Value;

            switch (o)
            {
                case Boolean b:
                    Writer.Append(b ? "true" : "false");
                    break;
                default:
                    var isRequiresQuotes = IsRequiresQuotes(o);
                    var converter = _stateProvider.GetTypeConverter(node.Type);
                    var str = converter.ConvertToInvariantString(o);
                    PrintString(str, isRequiresQuotes);
                    break;
            }
        }

        protected abstract void PrintString(String str, Boolean isInQuotes);
    }
}