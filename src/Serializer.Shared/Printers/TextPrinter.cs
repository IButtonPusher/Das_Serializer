﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Printers
{
    public abstract class TextPrinter : PrinterBase<String, Char, ITextRemunerable>
    {
        protected TextPrinter(//ITextRemunerable writer,
                              //ISerializerSettings settings,
                              ITypeInferrer typeInferrer,
                              INodeTypeProvider nodeTypes,
                              IObjectManipulator objectManipulator,
                              Char pathSeparator)
            : base(//settings, 
                typeInferrer, nodeTypes, 
                objectManipulator, false, pathSeparator)
        {
            //writer.Undispose();
            //Writer = writer;
            //_tabs = new StringBuilder();

            //_indenter = settings.Indentation;
            //_newLine = settings.NewLine;
            //_indentLength = _indenter.Length;
        }

        //protected String Tabs => _tabs.ToString();


        protected static Boolean IsRequiresQuotes(Object? o)
        {
            var oType = o?.GetType();
            if (oType == null)
                return false;
            return oType == Const.StrType || oType == typeof(DateTime) || oType.IsEnum;
        }

        //protected void NewLine(ITextRemunerable Writer)
        //{
        //    Writer.Append(_newLine);// + Tabs);
        //    Writer.AppendRepeatedly('\t', _tabCount);
        //}

        protected abstract void PrintChar(ITextRemunerable writer,
                                          Char c);

        protected sealed override void PrintFallback(Object? o,
                                                     ITextRemunerable Writer,
                                                     Type propType)
        {
            PrintPrimitive(o, Writer, o!.GetType());
        }

        protected abstract void PrintInteger(Object val,
                                             ITextRemunerable Writer);


       

        protected sealed override void PrintPrimitive(Object? o,
                                                      ITextRemunerable Writer,
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
                    Writer.Append((Boolean) o ? "true" : "false");
                    break;

                case TypeCode.String:
                    PrintString(o.ToString(), Writer);
                    break;


                case TypeCode.Char:
                    PrintChar(Writer, (Char) o);
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
                        PrintStringWithoutEscaping(o.ToString(), Writer);
                    else
                        PrintInteger(o, Writer);

                    break;


                case TypeCode.Single:

                    PrintReal(((Single) o).ToString(CultureInfo.InvariantCulture), Writer);
                    break;

                case TypeCode.Double:
                    PrintReal(((Double) o).ToString(CultureInfo.InvariantCulture), Writer);
                    break;

                case TypeCode.Decimal:
                    PrintReal(((Decimal) o).ToString(CultureInfo.InvariantCulture), Writer);
                    break;

                case TypeCode.DateTime:
                    PrintString(((DateTime) o).ToString(CultureInfo.InvariantCulture), Writer);
                    break;

                default:
                    var str = _typeInferrer.ConvertToInvariantString(o);
                    PrintString(str, Writer);
                    break;
            }
        }

        protected abstract void PrintReal(String str,
                                          ITextRemunerable Writer);

        protected abstract void PrintString(String str,
                                            ITextRemunerable Writer);

        protected abstract void PrintString(String str,
                                            Boolean isInQuotes,
                                            ITextRemunerable Writer);

        protected abstract void PrintStringWithoutEscaping(String str,
                                                           ITextRemunerable Writer);

        //protected virtual void TabIn()
        //{
        //    _tabCount--;
        //    //_tabs.Remove(0, _indentLength);
        //}

        //protected virtual void TabOut()
        //{
        //    _tabCount++;
        //    //_tabs.Append(_indenter);
        //}

        //protected readonly Stack<StackFormat> _formatStack;

        //protected readonly String _indenter;
        //private readonly Int32 _indentLength;
        //protected readonly String _newLine;

        //protected Int32 _tabCount;

        //private readonly StringBuilder _tabs;

        //protected readonly ITextRemunerable Writer;
    }
}
