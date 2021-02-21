using System;
using System.Text;

namespace Das.Serializer.Remunerators
{
    public class CompactStringSaver : StringSaver
    {
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
}
