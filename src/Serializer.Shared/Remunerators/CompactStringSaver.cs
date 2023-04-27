using System;
using System.Text;

namespace Das.Serializer.Remunerators;

public class CompactStringSaver : StringSaver
{
   public CompactStringSaver(String seed,
                             Action<StringSaver> notifyDispose)
      : base(seed, notifyDispose)
   {}

   public CompactStringSaver(Int32 length)
   {
      Capacity = length;
   }

   public CompactStringSaver(String seed) : base(seed)
   {
            
   }

   public CompactStringSaver()
   {
            
   }

   public sealed override void PrintCurrentTabs()
   {
            
   }

   public sealed override void TabIn()
   {
            
   }

   public sealed override void TabOut()
   {
            
   }

   public sealed override void NewLine()
   {
   }

   public sealed override void IndentRepeatedly(Int32 count)
   {
            
   }

   public static implicit operator StringBuilder(CompactStringSaver sv)
   {
      return sv._sb;
   }
}