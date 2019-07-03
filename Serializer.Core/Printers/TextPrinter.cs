using Das.Remunerators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Das.Serializer;
using Serializer;
using Serializer.Core.Printers;

namespace Das.Printers
{
	internal abstract class TextPrinter : PrinterBase<char>
	{
        protected TextPrinter(ITextRemunerable writer, ISerializationState stateProvider,
            ISerializerSettings settings) : base(stateProvider, settings)
        {
            Writer = writer;
            _tabs = new StringBuilder();
            _formatStack = new Stack<StackFormat>();
            _indenter = stateProvider.Settings.Indentation;
            _newLine = stateProvider.Settings.NewLine;
            _indentLength = _indenter.Length;
        }

        protected TextPrinter(ITextRemunerable writer, ISerializationState stateProvider) 
			: this(writer, stateProvider, stateProvider.Settings) { }

        protected String Tabs => _tabs.ToString();
        
        private readonly StringBuilder _tabs;
        protected readonly Stack<StackFormat> _formatStack;

        protected readonly ITextRemunerable Writer;

        protected readonly String _indenter;
        protected readonly String _newLine;
        private readonly Int32 _indentLength;
      

        protected static bool IsRequiresQuotes(object o)
		{
			var oType = o?.GetType();
			if (oType == null)
				return false;
			return oType == Const.StrType || oType == typeof(DateTime) || oType.IsEnum;
		}

		protected void TabOut() => _tabs.Append(_indenter);

        protected void TabIn() => _tabs.Remove(0, _indentLength);

        protected void NewLine() => Writer.Append(_newLine + Tabs);


        protected override void PrintFallback(PrintNode node)
		{
            node.Type = node.Value.GetType();
			PrintPrimitive(node);
		}

		/// <summary>
		/// xml puts all primitives as attributes and in quotes. Json does not put
		/// numeric types in quotes
		/// </summary>
		protected override void PrintPrimitive(PrintNode node)
		{
            var o = node.Value;
            var type = node.Type;

            if (type == typeof(Boolean))
			{
				Writer.Append((Boolean)o ? "true" : "false");
			}			
			else
			{				
				var isRequiresQuotes = IsRequiresQuotes(o);
				PrintString(TypeDescriptor.GetConverter(type)
					.ConvertToInvariantString(o), isRequiresQuotes);
			}			
		}
				
		protected abstract void PrintString(String str, Boolean isInQuotes);
	}
}
