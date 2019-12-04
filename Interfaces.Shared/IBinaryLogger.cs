using System;
using System.Diagnostics;

namespace Das.Serializer
{
    public interface BinaryLogger
    {
        void Debug(String val);

        void TabPlus();

        void TabMinus();

        String Tabs { get; }


    }
}
