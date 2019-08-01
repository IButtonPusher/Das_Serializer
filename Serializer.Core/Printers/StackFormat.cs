using System;

namespace Das.Printers
{
    internal class StackFormat
    {
        public Int32 Tabs { get; set; }
        public Boolean IsTagOpen { get; set; }

        public StackFormat(Int32 tabs, Boolean isTagOpen)
        {
            Tabs = tabs;
            IsTagOpen = isTagOpen;
        }
    }
}