using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Das.Serializer.Remunerators;

public abstract class StringBuilderWrapper : StringBuilderBase,
                                             ITextRemunerable
{
   public StringBuilderWrapper()
   {
      _sb = new StringBuilder();
   }

   public Int32 Capacity
   {
      get => _sb.Capacity;
      set => _sb.Capacity = value;
   }

   public Int32 Length => _sb.Length;

   public void Append(Char data1,
                      String data2)
   {
      _sb.Append(data1);
      _sb.Append(data2);
   }

   public void Append(ITextAccessor txt)
   {
      _sb.Append(txt);
   }

   void ITextRemunerable.Append(Char item)
   {
      _sb.Append(item);
   }

   [MethodImpl(256)]
   public void AppendRepeatedly(Char item,
                                Int32 count)
   {
      _sb.Append(item, count);
   }

   public bool Append<T>(IEnumerable<T> items,
                         Char separator) where T : IConvertible
   {
      _sb.Append(String.Join(separator.ToString(), items));
      return true;
   }

   public bool Append<T>(IEnumerable<T> items,
                         Char separator,
                         Int32 maxCount) where T : IConvertible
   {
      _sb.Append(String.Join(separator.ToString(), items.Take(maxCount)));

            
      return true;
   }

   public void AppendRight<TCollection, TData>(TCollection items,
                                               Char separator,
                                               Int32 maxCount)
      where TCollection : IEnumerable<TData>, ICollection
   {
      AppendRightImpl<TCollection, TData>(_sb, items, separator, maxCount);
   }

   public void Insert(Int32 index,
                      String str)
   {
      _sb.Insert(index, str);
   }

   public void Remove(Int32 startIndex,
                      Int32 length)
   {
      _sb.Remove(startIndex, length);
   }

   public abstract void PrintCurrentTabs();

   public abstract void TabIn();

   public abstract void TabOut();

   public abstract void NewLine();

   public abstract void IndentRepeatedly(Int32 count);


   public ITextAccessor ToImmutable()
   {
      return new StringAccessor(_sb.ToString());
   }

   public void Undispose()
   {
      //_textRemunerableImplementation.Undispose();
   }

   //void IRemunerable<string, char>.Append(Char data)
   [MethodImpl(256)]
   public void Append(Char data)
   {
      _sb.Append(data);
   }

   public Boolean IsEmpty => _sb.Length == 0;

   public Char this[Int32 index] => _sb[index];

   public String this[Int32 start,
                      Int32 end] =>
      _sb.ToString().Substring(start, end - start);


   [MethodImpl(256)]
   public void Append(String data)
   {
      _sb.Append(data);
   }

   public void Append(String data1,
                      String data2)
   {
      _sb.Append(data1);
      _sb.Append(data2);
   }

   public void Append(DateTime dt)
   {
      Append(dt, _sb);
            
   }

   public void Append(IEnumerable<string> datas)
   {
      _sb.Append(datas);
   }

   public void Append<T>(T data) where T : struct
   {
      _sb.Append(data);
   }

   public void Clear()
   {
      _sb.Clear();
   }

   public int IndexOf(String str,
                      Int32 startIndex)
   {
      return _sb.ToString().IndexOf(str, startIndex,
         StringComparison.Ordinal);
   }

   //public void Append(String data)
   //{
   //    _sb.Append(data);
   //}

   void IRemunerable<String>.Append(String str,
                                    Int32 cnt)
   {
      throw new NotSupportedException();
   }

   public void Dispose()
   {
      //_textRemunerableImplementation.Length = 0;
      //_textRemunerableImplementation.Clear();
   }

   public sealed override String ToString()
   {
      return _sb.ToString();
   }
}