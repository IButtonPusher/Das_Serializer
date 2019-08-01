using System;
using System.Collections.Generic;

namespace Das.Remunerators
{
    internal interface ITextRemunerable : IRemunerable<String, Char>, IStringRemunerable
    {
        void Append(Char data1, String data2);
    }

    internal interface IStringRemunerable : IRemunerable<String>
    {
        void Append(String data1, String data2);
        void Append(IEnumerable<String> datas);
    }
}