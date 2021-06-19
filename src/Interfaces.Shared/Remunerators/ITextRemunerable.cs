using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
   public interface ITextRemunerable : IRemunerable<String, Char>, IStringRemunerable
   {
      void Append(Char data1,
                  String data2);

      //void Append(ITextAccessor txt);

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
}
