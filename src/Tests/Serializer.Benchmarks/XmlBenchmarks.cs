using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using Serializer.Tests;

// ReSharper disable All

namespace Serializer.Benchmarks;

public class XmlBenchmarks
{
   static XmlBenchmarks()
   {
      Serializer = new DasSerializer();

      var simpleExample = SimpleClass.GetExample();
      SimpleExampleXml = Serializer.ToXml(simpleExample);

      var primitiveCollectionProp = SimpleClassWithPrimitiveCollection.GetExample();
      PrimitivePropXml = Serializer.ToXml(primitiveCollectionProp);

      var objectCollectionProp = SimpleClassWithObjectCollection.GetExample();
      ObjectPropXml = Serializer.ToXml(objectCollectionProp);
   }

   [Benchmark]
   public SimpleClassWithObjectCollection ObjectCollectionBaseline()
   {
      return Serializer.FromXml<SimpleClassWithObjectCollection>(ObjectPropXml);
   }

   [Benchmark]
   public SimpleClassWithObjectCollection ObjectCollectionExpress()
   {
      return Serializer.FromXml<SimpleClassWithObjectCollection>(ObjectPropXml);
   }

   [Benchmark]
   public SimpleClassWithPrimitiveCollection PrimitiveCollectionBaseline()
   {
      return Serializer.FromXml<SimpleClassWithPrimitiveCollection>(PrimitivePropXml);
   }

   [Benchmark]
   public SimpleClassWithPrimitiveCollection PrimitiveCollectionExpress()
   {
      return Serializer.FromXml<SimpleClassWithPrimitiveCollection>(PrimitivePropXml);
   }


   [Benchmark]
   public SimpleClass PrimitivePropertiesBaseline()
   {
      return Serializer.FromXml<SimpleClass>(SimpleExampleXml);
   }

   [Benchmark]
   public SimpleClass PrimitivePropertiesExpress()
   {
      return Serializer.FromXml<SimpleClass>(SimpleExampleXml);
   }

   private static DasSerializer Serializer;


   private static String SimpleExampleXml;
   private static String PrimitivePropXml;
   private static String ObjectPropXml;
}