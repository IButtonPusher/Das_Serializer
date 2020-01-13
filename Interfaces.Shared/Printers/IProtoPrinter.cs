using System;
using System.IO;
using Das.Serializer;

namespace Das.Printers
{
    public interface IProtoPrinter
    {
        void Print<TObject>(TObject o);

        Stream Stream
        {
            // ReSharper disable once UnusedMember.GlobalQueue
            get;
            set;
        }
    }
}