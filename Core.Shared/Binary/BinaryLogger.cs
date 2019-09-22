using System;
using System.Diagnostics;
using System.Linq;

namespace Serializer.Core.Binary
{
    internal class BinaryLogger
    {
        private Int32 _tabIndex;

        public String Tabs => String.Concat(Enumerable.Repeat('\t', _tabIndex));

        [Conditional("DEBUG")]
        public void TabPlus() => _tabIndex++;

        [Conditional("DEBUG")]
        public void TabMinus() => _tabIndex--;

        [Conditional("DEBUG")]
        public void Debug(String val)
        {
            Trace.WriteLine(Tabs + val);
        }
    }
}