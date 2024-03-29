﻿// ReSharper disable once RedundantUsingDirective

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Serializer.Tests;
using Serializer.Tests.ProtocolBuffers;

// ReSharper disable All

namespace Serializer.Benchmarks;

public class Program
{
   public static void Main(String[] args)
   {
      #if DEBUG
      //RunManyTimes();

      RunJsonManyTimes();
      //var jb = new JsonBenchmarks();
      //var rdrr = jb.JsonNetPrimitivePropertiesJson();


      //RunXmlManyTimes();

      #else
            if (args == null || args.Length == 0)
            //if (false)
            {
                //BenchmarkRunner.Run<JsonBenchmarks>();
                BenchmarkRunner.Run<ProtoBufTests>();
            }
            else
               RunManyTimes();
      #endif
   }

   private static void RunJsonManyTimes()
   {
      var bm = new JsonBenchmarks();
      //var wafl = bm.JsonNetPrimitivePropertiesJson();

      for (var c = 0; c < 10000; c++)
      {
         //bm.JsonNetDictionary();
         //bm.PrintDasDictionary();
                
         //bm.DasDictionary();
         //bm.DasPrimitiveProperties();
         //bm.DasPrimitiveProperties2();
         //bm.JsonNetPrimitiveProperties();
         //bm.DasPrintPrimitiveProperties2();
         //bm.PrimitivePropertiesJsonBaseline();
         //bm.PrimitivePropertiesJsonExpress();
      }
   }

   private unsafe static void RunManyTimes()
   {
      _rdrr = _rdrr ?? new Byte[256];

      var buff = new ProtoBufTests();
      var swo = new Stopwatch();
      swo.Start();


      for (var i = 0; i < 1000; i++)
      {
         {
            var sw = new Stopwatch();
            sw.Start();


            for (var c = 0; c < 10000; c++)
            {
               buff.DasSimpleMessage();
               buff.DasDoubleMessage();
               buff.DasStringMessage();
               buff.DasDictionary();
               buff.DasNegativeIntegerMessage();
               buff.DasMultiProperties();
               buff.DasComposedMessage();

               buff.DasByteArray();

               #if DEBUG

               buff.TypeProvider.DumpProxies();

               #endif
            }


            Debug.WriteLine(" das elapsed: " + sw.ElapsedMilliseconds);
         }
      }

      for (var i = 0; i < 1000; i++)
      {
      }


      Debug.WriteLine(" das TOTAL: " + swo.ElapsedMilliseconds);
   }

   private static void RunXmlManyTimes()
   {
      var bm = new XmlBenchmarks();

      for (var c = 0; c < 10000; c++)
      {
         bm.PrimitivePropertiesBaseline();
         bm.PrimitivePropertiesExpress();

         bm.PrimitiveCollectionBaseline();
         bm.PrimitiveCollectionExpress();

         bm.ObjectCollectionBaseline();
         bm.ObjectCollectionExpress();
      }
   }

   [ThreadStatic]
   private static Byte[]? _rdrr;


   public class Benchies : TestBase
   {
      public Benchies()
      {
         var mc1 = ObjectDictionary.Get();

         var xml = Serializer.ToXml(mc1);
         _xmlString = xml;
         _xmlEnumerable = xml.ToCharArray();
      }

      [Benchmark]
      public ObjectDictionary DeserializeCharArray()
      {
         return Serializer.FromXml<ObjectDictionary>(_xmlString);
      }

      //[Benchmark]
      //public ObjectDictionary DeserializeEnumerable()
      //{
      //    return Serializer.FromXml<ObjectDictionary>(_xmlEnumerable);
      //}

      private Char[] _xmlEnumerable;
      private String _xmlString;
   }
}