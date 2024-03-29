﻿using System;
using System.IO;
using System.Threading.Tasks;
using Das.Streamers;

namespace Das.Serializer;

public partial class DasCoreSerializer
{
   public T FromBytes<T>(Byte[] bytes)
   {
      using (var state = StateProvider.BorrowBinary(Settings))
      {
         var arr = new ByteArray(bytes);

         var f = GetFeeder(state, arr);
         var res = state.Scanner.Deserialize<T>(f);
         return res;
      }
   }

   public Object FromBytes(Byte[] bytes)
   {
      return FromBytes<Object>(bytes);
   }

   public T FromBytes<T>(FileInfo file)
   {
      using (var stream = file.OpenRead())
      {
         return FromBytes<T>(stream);
      }
   }

   public T FromBytes<T>(Stream stream)
   {
      using (var state = StateProvider.BorrowBinary(Settings))
      {
         var arr = new ByteStream(stream);
         var f = GetFeeder(state, arr);
         return state.Scanner.Deserialize<T>(f);
      }
   }

   private IBinaryFeeder GetFeeder(IBinaryContext state,
                                   IByteArray arr)
   {
      return new BinaryFeeder(state.PrimitiveScanner, state, arr, Settings);
   }
}