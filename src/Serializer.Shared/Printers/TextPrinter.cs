using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Printers
{
    public abstract class TextPrinter : PrinterBase
    {
        protected TextPrinter(ITextRemunerable writer,
                              ISerializerSettings settings,
                              ITypeInferrer typeInferrer,
                              INodeTypeProvider nodeTypes,
                              IObjectManipulator objectManipulator)
            : base(settings, typeInferrer, nodeTypes, objectManipulator)
        {
            writer.Undispose();
            Writer = writer;
            _tabs = new StringBuilder();
            _formatStack = new Stack<StackFormat>();
            _indenter = settings.Indentation;
            _newLine = settings.NewLine;
            _indentLength = _indenter.Length;
        }

        protected String Tabs => _tabs.ToString();


        protected static Boolean IsRequiresQuotes(Object? o)
        {
            var oType = o?.GetType();
            if (oType == null)
                return false;
            return oType == Const.StrType || oType == typeof(DateTime) || oType.IsEnum;
        }

        protected void NewLine()
        {
            Writer.Append(_newLine + Tabs);
        }

        protected override void PrintFallback(Object? o,
                                              Type propType)
        {
            PrintPrimitive(o, o!.GetType());
        }


        protected override void PrintPrimitive(Object? o,
                                               Type propType)
        {
            switch (o)
            {
                case Boolean b:
                    Writer.Append(b ? "true" : "false");
                    break;
                
                default:
                    var isRequiresQuotes = IsRequiresQuotes(o);
                    var str = _typeInferrer.ConvertToInvariantString(o!);
                    PrintString(str!, isRequiresQuotes);
                    break;
            }
        }

        protected abstract void PrintString(String str,
                                            Boolean isInQuotes);

        protected virtual void TabIn()
        {
            _tabs.Remove(0, _indentLength);
        }

        protected virtual void TabOut()
        {
            _tabs.Append(_indenter);
        }

        protected readonly Stack<StackFormat> _formatStack;

        protected readonly String _indenter;
        private readonly Int32 _indentLength;
        protected readonly String _newLine;

        private readonly StringBuilder _tabs;

        protected readonly ITextRemunerable Writer;
    }
}
