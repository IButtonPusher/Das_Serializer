using System;
using System.Globalization;
using System.Threading.Tasks;
using Das.Serializer;

namespace Das.Printers;

public abstract class TextPrinter : PrinterBase<String, Char, ITextRemunerable>
{
   protected TextPrinter(ITypeInferrer typeInferrer,
                         INodeTypeProvider nodeTypes,
                         IObjectManipulator objectManipulator,
                         Char pathSeparator,
                         ITypeManipulator typeManipulator)
      : base(typeInferrer, nodeTypes, 
         objectManipulator, false, pathSeparator, typeManipulator)
   {
   }

   protected static Boolean IsRequiresQuotes(Object? o)
   {
      var oType = o?.GetType();
      if (oType == null)
         return false;
      return oType == Const.StrType || oType == typeof(DateTime) || oType.IsEnum;
   }

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
               // ReSharper disable once RedundantSuppressNullableWarningExpression
               PrintStringWithoutEscaping(o.ToString()!, Writer);
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
                                                      ITextRemunerable writer);
}