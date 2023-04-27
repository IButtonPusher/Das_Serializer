using System;
using System.Text;
using System.Threading.Tasks;

namespace Das.Serializer;

public abstract class StringBuilderBase : StringBase
{
   public void Append(Boolean item)
   {
      _sb.Append(item ? "true" : "false");
   }

   public void Append(Byte item)
   {
      _sb.Append(item);
   }

   public void Append(Int16 item)
   {
      _sb.Append(item);
   }

   public void Append(UInt16 item)
   {
      _sb.Append(item);
   }

   public void Append(Int32 item)
   {
      _sb.Append(item);
   }

   public void Append(UInt32 item)
   {
      _sb.Append(item);
   }

   public void Append(Int64 item)
   {
      _sb.Append(item);
   }

   public void Append(UInt64 item)
   {
      _sb.Append(item);
   }

   public void Append(Single item)
   {
      _sb.Append(item);
   }

   public void Append(Double item)
   {
      _sb.Append(item);
   }

   public void Append(Decimal item)
   {
      _sb.Append(item);
   }

#pragma warning disable CS8618
   protected StringBuilder _sb;
#pragma warning restore CS8618
}