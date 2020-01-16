using System;
using System.IO;

namespace Das.Printers
{
    public interface IProtoPrinter
    {
        void Print<TObject>(TObject o);

        Stream Stream { get; set; }
    }
}