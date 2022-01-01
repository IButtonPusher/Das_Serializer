using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Das.Extensions;

// ReSharper disable UnusedMember.Global

namespace Das.Serializer
{
   public abstract class StringSaver : StringBase,
                                       ITextRemunerable,
                                       ITextAccessor
   {
      static StringSaver()
      {
         _sbLock = new Object();
         _sbPool = new List<StringBuilder>();
      }

      public StringSaver()
      {
         _sb = GetBackingBuilder();
      }

      public StringSaver(String seed)
      {
         _sb = null!;
         if (!TryGetBiggerBackingBuilder(seed.Length, ref _sb!))
            _sb = new StringBuilder(seed);
         else _sb.Append(seed);
      }

      public StringSaver(String seed,
                         Action<StringSaver> notifyDispose)
         : this(seed)
      {
         _notifyDispose = notifyDispose;
      }

      public Boolean Contains(String str,
                              StringComparison comparison)
      {
         return _sb.ToString().IndexOf(str, comparison) >= 0;
      }

      public Boolean IsNullOrWhiteSpace()
      {
         if (_sb.Length == 0)
            return true;

         for (var c = 0; c < _sb.Length; c++)
            if (!Char.IsWhiteSpace(_sb[c]))
               return false;

         return true;
      }

      public String[] Split()
      {
         return _sb.ToString().Split();
      }

      public String[] Split(Char[] separators,
                            StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
      {
         return _sb.ToString().Split(separators, options);
      }

      public String[] TrimAndSplit()
      {
         return _sb.ToString().Trim().Split();
      }

      public String Remove(ISet<Char> chars)
      {
         return Remove(chars, _sb.ToString());
      }

      public String Substring(Int32 start,
                              Int32 length)
      {
         return _sb.ToString(start, length);
      }

      public String Substring(Int32 start)
      {
         return _sb.ToString(start, _sb.Length - start);
      }

      public Boolean Contains(Char c)
      {
         for (var i = 0; i < _sb.Length; i++)
            if (_sb[i] == c)
               return true;

         return false;
      }

      public Boolean Contains(String str)
      {
         return _sb.ToString().IndexOf(str, StringComparison.Ordinal) >= 0;
      }

      public Int32 Count(Char c)
      {
         var cnt = 0;
         for (var i = 0; i < _sb.Length; i++)
            if (_sb[i] == c)
               cnt++;

         return cnt;
      }

      public void CopyTo(Int32 sourceIndex,
                         Char[] destination,
                         Int32 destinationIndex,
                         Int32 count)
      {
         _sb.CopyTo(sourceIndex, destination, destinationIndex, count);
      }

      public bool Append<T>(IEnumerable<T> items,
                            Char separator,
                            Int32 maxCount)
         where T : IConvertible
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

      public void Append(DateTime dt)
      {
         Append(dt, _sb);
      }


      void IRemunerable<String>.Append(String str,
                                       Int32 cnt)
      {
         throw new NotSupportedException();
      }

      [MethodImpl(256)]
      public Int32 IndexOf(String value,
                           Int32 startIndex)
      {
         var length = value.Length;
         var maxSearchLength = Length - length + 1;

         for (var i = startIndex; i < maxSearchLength; ++i)
         {
            if (this[i] != value[0])
               continue;

            var index = 1;
            while (index < length && this[i + index] == value[index])
               ++index;

            if (index == length)
               return i;
         }

         return -1;
      }

      public String this[Int32 start,
                         Int32 end] => _sb.ToString(start, end - start);

      [MethodImpl(256)]
      public void Append(String data)
      {
         var len = _sb.Length + data.Length;
         EnsureCapacity(len);

         _sb.Append(data);
      }

      [MethodImpl(256)]
      public void Append(String data1,
                         String data2)
      {
         var len = _sb.Length + data1.Length + data2.Length;
         EnsureCapacity(len);

         _sb.Append(data1);
         _sb.Append(data2);
      }


      [MethodImpl(256)]
      public void Append(Char data1,
                         String data2)
      {
         var len = _sb.Length + 1 + data2.Length;
         EnsureCapacity(len);

         _sb.Append(data1);
         _sb.Append(data2);
      }

      public void AppendRepeatedly(Char item,
                                   Int32 count)
      {
         if (count <= 0)
            return;
         EnsureCapacity(count);
         _sb.Append(item, count);
      }

      public Boolean Append<T>(IEnumerable<T> items,
                               Char separator)
         where T : IConvertible
      {
         using (var itar = items.GetEnumerator())
         {
            if (!itar.MoveNext())
               return false;

            _sb.Append(itar.Current);

            while (itar.MoveNext())
            {
               _sb.Append(separator);
               _sb.Append(itar.Current);
            }
         }

         return true;
      }

      public void Append(IEnumerable<String> datas)
      {
         foreach (var data in datas)
         {
            _sb.Append(data);
         }
      }

      [MethodImpl(256)]
      public void Append<T>(T data) where T : struct
      {
         _sb.Append(data.ToString());
      }

      [MethodImpl(256)]
      public void Insert(Int32 index,
                         String str)
      {
         EnsureCapacity(str);
         _sb.Insert(index, str);
      }

      public abstract void NewLine();

      public abstract void IndentRepeatedly(Int32 count);

      public ITextAccessor ToImmutable()
      {
         return new StringAccessor(_sb.ToString());
      }

      [MethodImpl(256)]
      public void Dispose()
      {
         Recycle(_sb);
         _sb = null!;
         _notifyDispose?.Invoke(this);
      }

      public void Undispose()
      {
         // ReSharper disable once ConstantNullCoalescingCondition
         _sb ??= GetBackingBuilder();
      }

      [MethodImpl(256)]
      public void Append(Char data)
      {
         _sb.Append(data);
      }

      // ReSharper disable once UnusedMember.Global
      public void Remove(Int32 startIndex,
                         Int32 length)
      {
         _sb.Remove(startIndex, length);
      }

      public abstract void PrintCurrentTabs();

      public abstract void TabIn();

      public abstract void TabOut();

      // ReSharper disable once ConditionIsAlwaysTrueOrFalse
      public Boolean IsEmpty => _sb == null || _sb.Length == 0;

      public Char this[Int32 index] => _sb[index];

      public Int32 Length => _sb.Length;

      public Int32 Capacity
      {
         get => _sb.Capacity;
         set => EnsureCapacity(value);
      }

      public void Clear()
      {
         // ReSharper disable once ConstantNullCoalescingCondition
         _sb ??= GetBackingBuilder();

         _sb.Clear();
      }

      public void Append(ITextAccessor txt)
      {
         var len = _sb.Length + 1 + txt.Length;
         EnsureCapacity(len);

         _sb.Append(txt);
      }

      [MethodImpl(256)]
      public void Append(Object? obj)
      {
         _sb.Append(obj);
      }

      [MethodImpl(256)]
      public void Append(String data1,
                         String data2,
                         String data3)
      {
         var len = _sb.Length + data1.Length + data2.Length + data3.Length;
         EnsureCapacity(len);

         _sb.Append(data1);
         _sb.Append(data2);
         _sb.Append(data3);
      }

      public Boolean Append<T>(IList<T> items,
                               Char separator)
      {
         if (items.Count == 0)
            return false;

         _sb.Append(items[0]);

         for (var c = 1; c < items.Count; c++)
         {
            _sb.Append(separator);
            _sb.Append(items[c]);
         }

         return true;
      }

      public void Append(String[] datas,
                         Char separator)
      {
         if (datas.Length == 0)
            return;

         var len = 0;
         EnsureCapacity(len);

         for (var c = 0; c < datas.Length; c++) len += datas[c].Length + 1;

         for (var c = 0; c < datas.Length - 1; c++)
         {
            _sb.Append(datas[c]);
            _sb.Append(separator);
         }

         _sb.Append(datas[datas.Length - 1]);
      }

      public String GetConsumingString()
      {
         _sb ??= GetBackingBuilder();
         var res = _sb.ToString();
         _sb.Clear();
         return res;
      }


      public static implicit operator StringBuilder(StringSaver sv)
      {
         return sv._sb;
      }

      public override String ToString()
      {
         return _sb.ToString();
      }

      public void TrimEnd()
      {
         for (var c = _sb.Length - 1; c >= 0; c--)
            if (_sb[c] == ' ')
               _sb.Remove(c, 1);
            else
               break;
      }

      [MethodImpl(256)]
      private void EnsureCapacity(String adding)
      {
         var len = _sb.Length + adding.Length;
         EnsureCapacity(len);
      }

      [MethodImpl(256)]
      private void EnsureCapacity(Int32 totalLength)
      {
         if (totalLength > _sb.Capacity)
            TryGetBiggerBackingBuilder(totalLength, ref _sb!);
      }


      private static StringBuilder GetBackingBuilder()
      {
         lock (_sbLock)
         {
            if (_sbPool.Count <= 0)
               return new StringBuilder();

            var sb = _sbPool[0];

            _sbPool.RemoveAt(0);
            return sb;
         }
      }

      private static void Recycle(StringBuilder sb)
      {
         var capacity = sb.Capacity;

         sb.Clear();

         lock (_sbLock)
         {
            if (_sbPool.Count >= MaximumPoolSize)
               return;

            if (sb.Capacity >= MaximumBuilderSize)
               return;

            var half = _sbPool.Count / 2.0;
            if (half.IsCloseToZero())
            {
               _sbPool.Add(sb);
               return;
            }

            var i = Convert.ToInt32(half);
            var current = _sbPool[i];
            if (current.Capacity > capacity)
            {
               for (; i >= 0; i--)
               {
                  current = _sbPool[i];
                  if (current.Capacity < capacity)
                  {
                     _sbPool.Insert(i + 1, sb);
                     return;
                  }
               }

               _sbPool.Insert(0, sb);
            }
            else
            {
               for (; i < _sbPool.Count; i++)
               {
                  current = _sbPool[i];
                  if (current.Capacity > capacity)
                  {
                     _sbPool.Insert(i - 1, sb);
                     return;
                  }
               }

               _sbPool.Add(sb);
            }
         }
      }

      private static Boolean TryGetBiggerBackingBuilder(Int32 capacity,
                                                        ref StringBuilder? sb)
      {
         var newI = -1;

         lock (_sbLock)
         {
            var half = _sbPool.Count / 2.0;

            
            if (half.IsCloseToZero())
               return false;
            var i = Convert.ToInt32(half);
            var current = _sbPool[i];
            if (current.Capacity > capacity)
               //we need smaller than average
               for (; i >= 0; i--)
               {
                  current = _sbPool[i];
                  if (current.Capacity >= capacity)
                     continue;

                  //ok that's too small now
                  newI = i + 1;
                  goto isHaveIt;
               }
            else
               //we need greater than average
               for (; i < _sbPool.Count; i++)
               {
                  current = _sbPool[i];
                  if (current.Capacity <= capacity)
                     continue;

                  //ok that's too big now
                  newI = i - 1;
                  goto isHaveIt;
               }

            isHaveIt:
            if (newI == -1)
               return false;

            var letsUse = _sbPool[newI];
            _sbPool.RemoveAt(newI);

            if (sb != null)
            {
               letsUse.Append(sb);
               Recycle(sb);
            }

            sb = letsUse;

            return true;
         }
      }

      private const Int32 MaximumPoolSize = 32;
      private const Int32 MaximumBuilderSize = 1048576;

      private static readonly Object _sbLock;
      private static readonly List<StringBuilder> _sbPool;
      private readonly Action<StringSaver>? _notifyDispose;
      protected StringBuilder _sb;
   }
}
