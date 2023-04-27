using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer;

public interface ITextRemunerable : IRemunerable<String, Char>,
                                    IStringRemunerable,
                                    IIntRemunerable
{
   void Append(Char data1,
               String data2);

   new void Append(Char item);


   void AppendRepeatedly(Char item,
                         Int32 count);

   Boolean Append<T>(IEnumerable<T> items,
                     Char separator)
      where T : IConvertible;

   Boolean Append<T>(IEnumerable<T> items,
                     Char separator,
                     Int32 maxCount)
      where T : IConvertible;

   void AppendRight<TCollection, TData>(TCollection items,
                                        Char separator,
                                        Int32 maxCount)
      where TCollection : IEnumerable<TData>, ICollection;

   void Insert(Int32 index,
               String str);

   void Remove(Int32 startIndex,
               Int32 length);

   void PrintCurrentTabs();

   void TabIn();

   void TabOut();

   void NewLine();

   void IndentRepeatedly(Int32 count);

   ITextAccessor ToImmutable();

   void Undispose();

   Int32 Capacity { get; set; }

   Int32 Length { get; }
}