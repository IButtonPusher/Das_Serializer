using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface IStringRemunerable : IRemunerable<String>
{
   new void Append(String data);

   void Append(String data1,
               String data2);

   void Append(DateTime dt);

   void Append(IEnumerable<String> datas);

   void Append<T>(T data) where T : struct;

   void Clear();

   Int32 IndexOf(String str,
                 Int32 startIndex);

   Char this[Int32 index] { get; }

   String this[Int32 start,
               Int32 end] { get; }
}