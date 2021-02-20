using System;
using System.Collections.Generic;
using System.Globalization;
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
            //_formatStack = new Stack<StackFormat>();
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

        protected sealed override void PrintFallback(Object? o,
                                              Type propType)
        {
            PrintPrimitive(o, o!.GetType());
        }


        protected override void PrintPrimitive(Object? o,
                                               Type propType)
        {
            if (ReferenceEquals(null, o))
            {
                Writer.Append("null");
                return;
            }

            var typeCode = Type.GetTypeCode(propType);

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    Writer.Append((Boolean)o ? "true" : "false");
                    break;

                case TypeCode.String:
                    PrintString(o.ToString());
                    break;

               
                case TypeCode.Char:
                    PrintChar((Char)o);
                    break;
                    
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:

                    if (propType.IsEnum)
                        PrintStringWithoutEscaping(o.ToString());
                    else
                        PrintInteger(o);

                    break;


                case TypeCode.Single:
                    
                    PrintReal(((Single)o).ToString(CultureInfo.InvariantCulture));
                    break;
                    
                case TypeCode.Double:
                    PrintReal(((Double)o).ToString(CultureInfo.InvariantCulture));
                    break;
                    
                case TypeCode.Decimal:
                    PrintReal(((Decimal)o).ToString(CultureInfo.InvariantCulture));
                    break;
                    
                case TypeCode.DateTime:
                    PrintString(((DateTime)o).ToString(CultureInfo.InvariantCulture));
                    break;

                default:
                    var str = _typeInferrer.ConvertToInvariantString(o);
                    PrintString(str);
                    break;
            }

            //switch (o)
            //{
            //    case Boolean b:
            //        Writer.Append(b ? "true" : "false");
            //        break;
                
            //    default:
            //        var isRequiresQuotes = IsRequiresQuotes(o);
            //        var str = _typeInferrer.ConvertToInvariantString(o!);
            //        PrintString(str!, isRequiresQuotes);
            //        break;
            //}
        }

        protected abstract void PrintString(String str);

        protected abstract void PrintStringWithoutEscaping(String str);

        protected abstract void PrintChar(Char c);

        protected abstract void PrintString(String str,
                                            Boolean isInQuotes);

        protected abstract void PrintInteger(Object val);

        protected abstract void PrintReal(String str);

        protected virtual void TabIn()
        {
            _tabs.Remove(0, _indentLength);
        }

        protected virtual void TabOut()
        {
            _tabs.Append(_indenter);
        }

        //protected readonly Stack<StackFormat> _formatStack;

        protected readonly String _indenter;
        private readonly Int32 _indentLength;
        protected readonly String _newLine;

        private readonly StringBuilder _tabs;

        protected readonly ITextRemunerable Writer;
    }
}
