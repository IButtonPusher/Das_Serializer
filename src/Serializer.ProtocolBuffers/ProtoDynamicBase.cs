﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Das.Serializer.Remunerators;

namespace Das.Serializer.ProtoBuf;

public abstract class ProtoDynamicBase<T> : ProtoDynamicBase, IProtoProxy<T>
{
   protected ProtoDynamicBase(
      IProtoProvider proxyProvider)
      : base(proxyProvider)
   {
   }

   public abstract void Print(T obj,
                              Stream target);

   public T Scan(Stream stream)
   {
      return Scan(stream, stream.Length);
   }

   public abstract T Scan(Stream stream,
                          Int64 byteCount);


   public virtual T BuildDefault()
   {
      throw new NotSupportedException();
   }
}

public abstract class ProtoDynamicBase : ProtoBufWriter
{
   static ProtoDynamicBase()
   {
      Utf8 = Encoding.UTF8;
   }


   protected ProtoDynamicBase(IProtoProvider proxyProvider)
   {
      _proxyProvider = proxyProvider;
   }

   public abstract Boolean IsReadOnly { get; }

   public static void AddPacked16<TCollection>(TCollection target,
                                               Stream stream,
                                               Int32 bytesToUse)
      where TCollection : ICollection<Int16>
   {
      var end = stream.Position + bytesToUse;

      while (end > stream.Position)
         target.Add((Int16) GetInt32(stream));
   }

   public static void AddPacked32<TCollection>(TCollection target,
                                               Stream stream,
                                               Int32 bytesToUse)
      where TCollection : ICollection<Int32>
   {
      var end = stream.Position + bytesToUse;

      while (end > stream.Position)
         target.Add(GetInt32(stream));
   }

   public static void AddPacked64<TCollection>(TCollection target,
                                               Stream stream,
                                               Int32 bytesToUse)
      where TCollection : ICollection<Int64>
   {
      var end = stream.Position + bytesToUse;

      while (end > stream.Position)
         target.Add(GetInt64(stream));
   }

   public static void CopyMemoryStream(NaiveMemoryStream copyFrom,
                                       Stream destination)
   {
      destination.Write(copyFrom._buffer, 0, copyFrom.IntLength);
   }

   public void DebugWriteline(Object obj1,
                              object obj2)
   {
      Debug.WriteLine("Debug " + obj1 + "\t" + obj2);
   }

   public static IEnumerable<Int16> ExtractPacked16(Stream stream,
                                                    Int32 bytesToUse)
   {
      var end = stream.Position + bytesToUse;

      while (end > stream.Position)
         yield return (Int16) GetInt32(stream);
   }

   public static IEnumerable<Int32> ExtractPacked32(Stream stream,
                                                    Int32 bytesToUse)
   {
      var end = stream.Position + bytesToUse;

      while (end > stream.Position)
         yield return GetInt32(stream);
   }

   public static IEnumerable<Int64> ExtractPacked64(Stream stream,
                                                    Int32 bytesToUse)
   {
      var end = stream.Position + bytesToUse;

      while (end > stream.Position)
         yield return GetInt64(stream);
   }

   // ReSharper disable once UnusedMember.Global
   public static IEnumerable<TChild> GetChildren<TChild>(Stream stream,
                                                         IProtoProxy<TChild> proxy)
   {
      var nextItemLength = GetPositiveInt32(stream);

      if (nextItemLength > stream.Length) yield break; //TODO:

      var positionWas = stream.Position;
      var endPosition = positionWas + nextItemLength;

      while (positionWas < endPosition && endPosition <= stream.Length)
      {
         yield return proxy.Scan(stream, nextItemLength);
         positionWas = stream.Position;
      }
   }

   public static Int32 GetColumnIndex(Stream stream)
   {
      var result = 0;
      var push = 0;

      while (true)
      {
         var currentByte = stream.ReadByte();
         if (currentByte == -1)
            return 0;

         result += (currentByte & 0x7F) << push;

         push += 7;
         if ((currentByte & 0x80) == 0)
            return result >> 3;
      }
   }

   public static Int32 GetInt32(Stream stream)
   {
      var result = 0;
      var push = 0;

      while (true)
      {
         var currentByte = stream.ReadByte();

         result += (currentByte & 0x7F) << push;
         if (push == 28 && result < 0)
         {
            stream.Position += 5;
            return result;
         }

         push += 7;
         if ((currentByte & 0x80) == 0)
            return result;
      }
   }

   public static DateTime GetDateTime(Stream stream)
   {
      return DateTime.FromFileTime(GetInt64(stream));
   }

   public static Int64 GetInt64(Stream stream)
   {
      var result = 0L;
      var push = 0;

      while (true)
      {
         var currentByte = (Int64) stream.ReadByte();

         result += (currentByte & 0x7F) << push;
         if (push == 28 && result < 0)
         {
            stream.Position += 5;
            return result;
         }

         push += 7;
         if ((currentByte & 0x80) == 0)
            return result;
      }
   }

   public static Int32 GetPositiveInt32(Stream stream)
   {
      var result = 0;
      var push = 0;

      while (true)
      {
         var currentByte = stream.ReadByte();

         result += (currentByte & 0x7F) << push;

         push += 7;
         if ((currentByte & 0x80) == 0)
            return result;
      }
   }

   public static Int64 GetPositiveInt64(Stream stream)
   {
      var result = 0L;
      var push = 0;

      while (true)
      {
         var currentByte = stream.ReadByte();

         result += (currentByte & 0x7F) << push;

         push += 7;
         if ((currentByte & 0x80) == 0)
            return result;
      }
   }

   public static UInt32 GetUInt32(Stream stream)
   {
      UInt32 result = 0;
      var push = 0;

      while (true)
      {
         var currentByte = stream.ReadByte();

         result += (UInt32) (currentByte & 0x7F) << push;

         push += 7;
         if ((currentByte & 0x80) == 0)
            return result;
      }
   }

   public static UInt64 GetUInt64(Stream stream)
   {
      UInt64 result = 0L;
      var push = 0;

      while (true)
      {
         var currentByte = stream.ReadByte();

         result += (UInt64) (currentByte & 0x7F) << push;

         push += 7;
         if ((currentByte & 0x80) == 0)
            return result;
      }
   }

   public static BindingFlags InstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

   protected static Encoding Utf8;

   // ReSharper disable once UnusedMember.Global
   [ThreadStatic]
   protected static Byte[]? _readBytes;

   // ReSharper disable once NotAccessedField.Global
   protected readonly IProtoProvider _proxyProvider;
}