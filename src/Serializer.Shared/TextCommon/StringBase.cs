using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Das.Serializer
{
   public abstract class StringBase
   {
      protected static String Remove(ISet<Char> chars,
                                     String from)
      {
         if (String.IsNullOrEmpty(from) || chars.Count == 0)
            return from;

         var isNeeded = false;
         for (var c = 0; c < from.Length; c++)
            if (chars.Contains(from[c]))
            {
               isNeeded = true;
               break;
            }

         if (!isNeeded)
            return from;

         return String.Concat(ExtractSurvivors());

         IEnumerable<Char> ExtractSurvivors()
         {
            for (var c = 0; c < from.Length; c++)
            {
               var f = from[c];
               if (!chars.Contains(f))
                  yield return f;
            }
         }
      }

      
      protected static void AppendRightImpl<TCollection, TData>(StringBuilder _sb,
                                                         TCollection items,
                                                         Char separator,
                                                         Int32 maxCount)
         where TCollection : IEnumerable<TData>, ICollection
      {
         if (items.Count == 0)
            return;

         if (items.Count <= maxCount)
         {
            _sb.Append(String.Join(separator.ToString(), items));
         }
         else
         {
            var skip = items.Count - maxCount;
            var skipped = 0;

            using (var itar = items.GetEnumerator())
            {
               if (!itar.MoveNext() || ++skipped <= skip)
                  return;

               _sb.Append(itar.Current);

               while (itar.MoveNext())
               {
                  _sb.Append(separator);
                  _sb.Append(itar.Current);
               }
            }
         }
      }

      //protected static void AppendRightImpl<TCollection>(StringBuilder _sb,
      //                                                   TCollection items,
      //                                                   Char separator,
      //                                                   Int32 maxCount)
      //   where TCollection : IEnumerable, ICollection
      //{
      //   if (items.Count == 0)
      //      return;

      //   if (items.Count <= maxCount)
      //   {
      //      _sb.Append(String.Join(separator.ToString(), items));
      //   }
      //   else
      //   {
      //      var skip = items.Count - maxCount;
      //      var skipped = 0;

      //      var itar = items.GetEnumerator();

      //      if (!itar.MoveNext() || ++skipped <= skip)
      //         return;

      //      _sb.Append(itar.Current);

      //      while (itar.MoveNext())
      //      {
      //         _sb.Append(separator);
      //         _sb.Append(itar.Current);
      //      }
      //   }
      //}

      [MethodImpl(256)]
      protected static void Append(DateTime dt,
                                   StringBuilder sb)
      {
         sb.Append(dt.Year);
         sb.Append('-');
         AppendTwo(dt.Month, sb);
         sb.Append('-');
         AppendTwo(dt.Day, sb);

         sb.Append('T');
         AppendTwo(dt.Hour, sb);
         sb.Append(':');
         AppendTwo(dt.Minute, sb);
         sb.Append(':');
         AppendTwo(dt.Second, sb);
      }

      [MethodImpl(256)]
      private static void AppendTwo(Int32 val,
                                    StringBuilder sb)
      {
         switch (val)
         {
            case 0:
               sb.Append('0');
               sb.Append('0');
               return;

            case 1:
               sb.Append("01");
               return;

            case 2:
               sb.Append("02");
               return;

            case 3:
               sb.Append("03");
               return;

            case 4:
               sb.Append("04");
               return;

            case 5:
               sb.Append("05");
               return;

            case 6:
               sb.Append("06");
               return;

            case 7:
               sb.Append("07");
               return;

            case 8:
               sb.Append("08");
               return;

            case 9:
               sb.Append("09");
               return;

            case 10:
               sb.Append("10");
               return;


            case 11:
               sb.Append("11");
               return;

            case 12:
               sb.Append("12");
               return;

            case 13:
               sb.Append("13");
               return;

            case 14:
               sb.Append("14");
               return;

            case 15:
               sb.Append("15");
               return;

            case 16:
               sb.Append("16");
               return;

            case 17:
               sb.Append("17");
               return;

            case 18:
               sb.Append("18");
               return;

            case 19:
               sb.Append("19");
               return;

            case 20:
               sb.Append("20");
               return;


            case 21:
               sb.Append("21");
               return;

            case 22:
               sb.Append("22");
               return;

            case 23:
               sb.Append("23");
               return;

            case 24:
               sb.Append("24");
               return;

            case 25:
               sb.Append("25");
               return;

            case 26:
               sb.Append("26");
               return;

            case 27:
               sb.Append("27");
               return;

            case 28:
               sb.Append("28");
               return;

            case 29:
               sb.Append("29");
               return;


            case 30:
               sb.Append("30");
               return;

            case 31:
               sb.Append("31");
               return;

            case 32:
               sb.Append("32");
               return;

            case 33:
               sb.Append("33");
               return;

            case 34:
               sb.Append("34");
               return;

            case 35:
               sb.Append("35");
               return;

            case 36:
               sb.Append("36");
               return;

            case 37:
               sb.Append("37");
               return;

            case 38:
               sb.Append("38");
               return;

            case 39:
               sb.Append("39");
               return;


            case 40:
               sb.Append("40");
               return;

            case 41:
               sb.Append("41");
               return;

            case 42:
               sb.Append("42");
               return;

            case 43:
               sb.Append("43");
               return;

            case 44:
               sb.Append("44");
               return;

            case 45:
               sb.Append("45");
               return;

            case 46:
               sb.Append("46");
               return;

            case 47:
               sb.Append("47");
               return;

            case 48:
               sb.Append("48");
               return;

            case 49:
               sb.Append("49");
               return;


            case 50:
               sb.Append("50");
               return;

            case 51:
               sb.Append("51");
               return;

            case 52:
               sb.Append("52");
               return;

            case 53:
               sb.Append("53");
               return;

            case 54:
               sb.Append("54");
               return;

            case 55:
               sb.Append("55");
               return;

            case 56:
               sb.Append("56");
               return;

            case 57:
               sb.Append("57");
               return;

            case 58:
               sb.Append("58");
               return;

            case 59:
               sb.Append("59");
               return;
         }
      }
   }
}
