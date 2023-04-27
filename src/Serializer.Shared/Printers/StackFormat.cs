using System;
using System.Threading.Tasks;

namespace Das.Printers;

public class StackFormat
{
   public StackFormat(Int32 tabs,
                      Boolean isTagOpen)
   {
      Tabs = tabs;
      IsTagOpen = isTagOpen;
   }

   public Boolean IsTagOpen { get; set; }

   public Int32 Tabs { get; set; }
}