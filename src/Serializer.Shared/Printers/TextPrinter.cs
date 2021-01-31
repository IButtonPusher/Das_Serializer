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
                              //ISerializationState stateProvider,
                              ISerializerSettings settings,
                              ITypeInferrer typeInferrer,
                              INodeTypeProvider nodeTypes,
                              IObjectManipulator objectManipulator)
            : base(//stateProvider, 
                settings, typeInferrer, nodeTypes, objectManipulator)
        {
            writer.Undispose();
            Writer = writer;
            _tabs = new StringBuilder();
            _formatStack = new Stack<StackFormat>();
            _indenter = settings.Indentation;
            _newLine = settings.NewLine;
            _indentLength = _indenter.Length;
        }

        //protected TextPrinter(ITextRemunerable writer,
        //                      ISerializationState stateProvider,
        //                      ITypeInferrer typeInferrer)
        //    : this(writer, stateProvider, stateProvider.Settings,
        //        typeInferrer)
        //{
        //}

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


        //protected override void PrintFallback(IPrintNode node)
        //{
        //    node.Type = node.Value!.GetType();
        //    PrintPrimitive(node);
        //}

        protected override void PrintFallback(Object? o,
                                              Type propType)
        {
            PrintPrimitive(o, o!.GetType());
        }

        ///// <summary>
        /////     xml puts all primitives as attributes and in quotes. Json does not put
        /////     numeric types in quotes
        ///// </summary>
        //protected override void PrintPrimitive(IPrintNode node)
        //{
        //    var o = node.Value;

        //    switch (o)
        //    {
        //        case Boolean b:
        //            Writer.Append(b ? "true" : "false");
        //            break;
        //        default:
        //            var isRequiresQuotes = IsRequiresQuotes(o);
        //            var converter = _typeInferrer.GetTypeConverter(node.Type!);
        //            var str = converter.ConvertToInvariantString(o!);
        //            PrintString(str!, isRequiresQuotes);
        //            break;
        //    }
        //}

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
                    var converter = _typeInferrer.GetTypeConverter(propType);
                    var str = converter.ConvertToInvariantString(o!);
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
