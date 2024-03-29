﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Das.Serializer;
using Serializer.Tests.TestTypes;
using Xunit;

#pragma warning disable 8602

// ReSharper disable All

namespace Serializer.Tests.Xml;

public class XmlTests : TestBase
{
   private DasSerializer GetTypeSpecifyingSerializer()
   {
      return new DasSerializer
      (
         new DasSettings
         {
            TypeSpecificity = TypeSpecificity.All
         });
   }


   [Fact]
   public void AnonymousTypeXml()
   {
      var vvq = GetAnonymousObject();

      //var srl = new DasSerializer();
      var xml = srl.ToXml(vvq);
      var res = srl.FromXml(xml);

      var isOk = SlowEquality.AreEqual(res, vvq);
      Assert.True(isOk);
   }

   [Fact]
   public void BlockingXml()
   {
      var bc = new BlockingCollection<SimpleClassObjectProperty>();
      bc.Add(SimpleClassObjectProperty.GetPrimitivePayload());
      bc.Add(SimpleClassObjectProperty.GetNullPayload());

      var x = Serializer.ToXml(bc);
      var res = Serializer.FromXml<BlockingCollection<SimpleClassObjectProperty>>(x);

      for (var i = 0; i < bc.Count; i++)
      {
         Assert.True(bc.Skip(i).First().Equals(res.Skip(i).First()));
      }
   }

   [Fact]
   public void CircularReferencesXml()
   {
      var sc1 = Teacher.Get();
      //var srl = new DasSerializer();

      //restore the reference
      srl.Settings.CircularReferenceBehavior = CircularReference.SerializePath;
      var xml = srl.ToXml(sc1);
      var sc2 = srl.FromXml<Teacher>(xml);

      var sc1FirstPupil = sc1.Pupils.First();
      var sc2FirstPupil = sc2.Pupils.First();

      Assert.True(sc1.FirstName == sc2.FirstName && sc1FirstPupil.MiddleName ==
         sc2FirstPupil.MiddleName && sc1FirstPupil.MathTeacher.FirstName ==
         sc2FirstPupil.MathTeacher.FirstName);


      //lose the reference
      srl.Settings.CircularReferenceBehavior = CircularReference.IgnoreObject;
      xml = srl.ToXml(sc1);
      sc2 = srl.FromXml<Teacher>(xml);
      var badProp = "";

      sc1FirstPupil = sc1.Pupils.First();
      sc2FirstPupil = sc2.Pupils.First();

      Assert.True(sc1.FirstName == sc2.FirstName && sc1FirstPupil.MiddleName ==
         sc2FirstPupil.MiddleName && sc2FirstPupil.MathTeacher == null);

      sc2 = srl.FromXml<Teacher>(xml);

      Assert.False(SlowEquality.AreEqual(sc1, sc2, ref badProp));


      //fail
      srl.Settings.CircularReferenceBehavior = CircularReference.ThrowException;
      try
      {
         srl.ToXml(sc1);
         Assert.True(false);
      }
      catch (CircularReferenceException)
      {
         Assert.True(true);
      }
      catch
      {
         Assert.True(false);
      }
   }

   [Fact]
   public void ClassWithDictionaryXml()
   {
      var mc1 = ObjectDictionary.Get();
      IDictionary idic = mc1.Dic;

      var xml = Serializer.ToXml(mc1);

      var mc2 = Serializer.FromXml<ObjectDictionary>(xml);

      if (mc1 == null || mc2 == null)
         Assert.False(true);
      if (mc1.Dic.Count != mc2.Dic.Count)
         Assert.False(true);
      Assert.True(Serializer.ToXml(mc2) == xml);

      Thread.CurrentThread.CurrentCulture = new CultureInfo("DE-de");

      xml = Serializer.ToXml(mc1);

      mc2 = Serializer.FromXml<ObjectDictionary>(xml);

      if (mc1 == null || mc2 == null)
         Assert.False(true);
      if (mc1.Dic.Count != mc2.Dic.Count)
         Assert.False(true);
      Assert.True(Serializer.ToXml(mc2) == xml);
   }

   [Fact]
   public void ClassWithObjectArrayXml()
   {
      var arr1 = ObjectArray.Get();

      var xml = Serializer.ToXml(arr1);
      var res = Serializer.FromXml<ObjectArray>(xml);

      Assert.True(arr1.ItemArray[0].Equals(res.ItemArray[0]));
      Assert.True(arr1.ItemArray[1].Equals(res.ItemArray[1]));
   }

   [Fact]
   public void ClassWithPrimitiveArrayXml()
   {
      var mc1 = PrimitiveArray.Get();

      var xml = Serializer.ToXml(mc1);
      var res = Serializer.FromXml<PrimitiveArray>(xml);
      Assert.True(mc1.StringArray.SequenceEqual(res.StringArray));
   }

   [Fact]
   public void ConcurrentDictionaryXml()
   {
      var mc1 = ObjectConcurrentDictionary.Get();
      var xml = Serializer.ToXml(mc1);

      var mc2 = Serializer.FromXml<ObjectConcurrentDictionary>(xml);


      if (mc1 == null || mc2 == null)
         Assert.False(true);
      if (mc1.Dic.Count != mc2.Dic.Count)
         Assert.False(true);
      Assert.True(Serializer.ToXml(mc2) == xml);
   }

   [Fact]
   public void EmptyDictionaryXml()
   {
      var mc1 = new ObjectDictionary();
      var xml = Serializer.ToXml(mc1);

      var mc2 = Serializer.FromXml<ObjectDictionary>(xml);


      if (mc1 == null || mc2 == null)
         Assert.False(true);
      if (mc1.Dic.Count != mc2.Dic.Count)
         Assert.False(true);

      var doc = new XmlDocument();
      doc.LoadXml(xml);

      Assert.True(Serializer.ToXml(mc2) == xml);
   }

   [Fact]
   public void EmptyStringIsNotNull()
   {
      var eg = SimpleClass.GetExample<SimpleClass>();
      eg.Name = string.Empty;

      var xml = Serializer.ToXml(eg);
      var eg2 = Serializer.FromXml<SimpleClass>(xml);

      Assert.NotNull(eg2.Name);
   }

   [Fact]
   public void EncodingNode()
   {
      var v = VersionContainer.TestInstance;
      var std = new DasSerializer();
      var xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" + std.ToXml(v);


      var v2 = std.FromXml<VersionContainer>(xml);

      var areEqual = SlowEquality.AreEqual(v, v2);

      Assert.True(areEqual);
   }

   [Fact]
   public void GdiColorInferredXml()
   {
      Serializer.Settings.TypeNotFoundBehavior = TypeNotFoundBehavior.ThrowException;

      var clr = Color.Purple;
      Serializer.Settings.TypeSpecificity = TypeSpecificity.All;
      var xml = Serializer.ToXml(clr);
      var diesel = Serializer.FromXml(xml);
      var yeti = (Color) diesel;
      Serializer.Settings.TypeSpecificity = TypeSpecificity.Discrepancy;
      Assert.True(clr.R == yeti.R && clr.G == yeti.G && clr.B == yeti.B);
   }

   [Fact]
   public void GuidPropertyImplicitImplementation()
   {
      var user = new User
      {
         UniqueId = Guid.NewGuid(),
         id = 8675
      };

      var xml = Serializer.ToXml(user);

      var user2 = Serializer.FromXml<User>(xml);
      //var user3 = Serializer.FromXmlEx<User>(xml);

      var res = SlowEquality.AreEqual(user, user2);
      //&& SlowEquality.AreEqual(user3, user2);
      Assert.True(res);
   }

   [Fact]
   public void HeadlineCollection()
   {
      var fi = new FileInfo(Path.Combine(
         AppDomain.CurrentDomain.BaseDirectory,
         "Xml",
         "Headlines.txt"));

      var xml = File.ReadAllText(fi.FullName);
      //var srl = new DasSerializer();
      var count = 0;

      foreach (var a in srl.FromXmlItems<ValueArticleDto>(xml))
      {
         count++;
      }

      Assert.True(count == 100);
   }

   [Fact]
   public void ExplicitImplementation()
   {
      var ei = new ExplicitImplementation();

      var xml = srl.ToXml(ei);

      var eio = srl.FromXml<ExplicitImplementation>(xml);

      Assert.True(SlowEquality.AreEqual(ei, eio));
   }

   [Fact]
   public void Int32asInt16Xml()
   {
      var someInt = 55;

      //var srl = new DasSerializer();
      srl.Settings.TypeSpecificity = TypeSpecificity.All;
      var xml = srl.ToXml<Int16>(someInt);
      ////Debug.WriteLine("xml = " + xml);
      var int2 = srl.FromXml<Int16>(xml);

      var int3 = srl.FromXml<Int32>(xml);

      var int4 = (Int16) srl.FromXml(xml);

      Assert.True(someInt == int2 && int2 == int3 && int2 == int4);
   }

   [Fact]
   public void IntExplicitXml()
   {
      var someInt = 55;
            
      var xml = srl.ToXml(someInt);

      var int2 = srl.FromXml<Int32>(xml);
      Assert.True(someInt == int2);
   }

   [Fact]
   public void ListsXml()
   {
      var list1 = ObjectList.Get();

      var xml = Serializer.ToXml(list1);
      var res = Serializer.FromXml<ObjectList>(xml);

      for (var i = 0; i < list1.ItemList.Count; i++)
      {
         Assert.True(list1.ItemList[i].Equals(res.ItemList[i]));
      }

      ////////////

      var list2 = PrimitiveList.Get();

      xml = Serializer.ToXml(list2);
      var dres = Serializer.FromXml<PrimitiveList>(xml);

      for (var i = 0; i < list2.DecimalList.Count; i++)
      {
         Assert.True(list2.DecimalList[i].Equals(dres.DecimalList[i]));
      }
   }

   [Fact]
   public void NoEmptyConstructorNullValueTypeValue()
   {
      var dto = new ArticleDto(1, "bob", "low", 2, null, 12344, "hello", -500);
            
      var xml = srl.ToXml(dto);
      var dto2 = srl.FromXml<ArticleDto>(xml);

      Assert.True(SlowEquality.AreEqual(dto, dto2));
   }

   [Fact]
   public void ObjectPropertiesXml()
   {
      var test = TestCompositeClass.Init();

      //var srl = new DasSerializer();
      var xml = srl.ToXml(test);

      var sc2 = srl.FromXml<TestCompositeClass>(xml);
      var badProp = "";
      Assert.True(SlowEquality.AreEqual(test, sc2, ref badProp));
   }

   [Fact]
   public void ObjectReferenceTypeXml()
   {
      var sc = SimpleClassObjectProperty.GetNullPayload();
      sc.Payload = SimpleClassObjectProperty.GetPrimitivePayload();

      var xml = srl.ToXml(sc);

      var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
      var badProp = "";
      Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));

      sc = SimpleClassObjectProperty.GetStringPayload();
      xml = srl.ToXml(sc);
      sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
      Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
   }

   [Fact]
   public void PrimitivePropertiesNoAttributes()
   {
      var sc = SimpleClassObjectProperty.GetNullPayload();

      var settings = DasSettings.CloneDefault();
      settings.IsUseAttributesInXml = false;
      var srl = new DasSerializer(settings);
      var xml = srl.ToXml(sc);

      var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
      var badProp = "";
      Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
   }

   [Fact]
   public void PrimitivePropertiesXml()
   {
      var sc = SimpleClassObjectProperty.GetNullPayload();

      //var srl = new DasSerializer();
      var xml = srl.ToXml(sc);

      var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
      var badProp = "";
      Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
   }

   [Fact]
   public void QueuesXml()
   {
      //var qs = new Queue<SimpleClassObjectProperty>();
      //qs.Enqueue(SimpleClassObjectProperty.GetPrimitivePayload());

      //var x = Serializer.ToXml(qs);
      //var qs2 = Serializer.FromXml<Queue<SimpleClassObjectProperty>>(x);

      var qc = new ConcurrentQueue<SimpleClassObjectProperty>();
      qc.Enqueue(SimpleClassObjectProperty.GetNullPayload());

      var xx = Serializer.ToXml(qc);
      var qc2 = Serializer.FromXml<ConcurrentQueue<SimpleClassObjectProperty>>(xx);
   }

   [Fact]
   public async Task SuperfluousNamespaceAttribute()
   {
      var fi = new FileInfo(Path.Combine(
         AppDomain.CurrentDomain.BaseDirectory,
         "Xml", "ExtraAttributes.xml"));

      //var eg = SimpleClass.GetExample<SimpleClass>();


      //var xml = Serializer.ToXml(eg);

      //xml = xml.Replace("<ShiftPreference>09:00:00</ShiftPreference>",
      //    "<ShiftPreference xmlns=\"http://tempuri.org/\">09:00:00</ShiftPreference>");

      var eg2 = await Serializer.FromXmlAsync<SimpleClass>(fi);
   }

   [Fact]
   public async Task SvgTestAsync()
   {
      var settings = DasSettings.CloneDefault();
      settings.IsPropertyNamesCaseSensitive = false;
      var srl = new DasSerializer(settings);
      var fi = new FileInfo(Path.Combine(
         AppDomain.CurrentDomain.BaseDirectory,
         "Xml",
         "cog.svg"));
      var doc = await srl.FromXmlAsync<SvgDocument>(fi);

      Assert.NotNull(doc.Path);

      //fi = new FileInfo(Path.Combine(
      //    AppDomain.CurrentDomain.BaseDirectory,
      //    "Xml",
      //    "book.svg"));

      //doc = await srl.FromXmlAsync<SvgDocument>(fi);
   }

   [Fact]
   public void TimespanXml()
   {
      var dyn = new
      {
         //StartedAt = new TimeSpan(12, 34, 56)
         StartedAt = new DateTime(2000, 1, 1)
      };
      //var ts = new TimeSpan(12, 34, 56);
      //var srl = new DasSerializer();
      var xml = srl.ToXml(dyn);

      var res = srl.FromXml(xml);
      //Assert.True(dyn.StartedAt == int2);
   }

   [Fact]
   public void ImplicitCastableProperty()
   {
      var pit1 = new PropertyImplicitToCtorParam1(new ImplicitlyConvertible1());
      var xml = srl.ToXml(pit1);
      xml = xml.Replace("PropertyImplicitToCtorParam1", "PropertyImplicitToCtorParam2");

      var pit2 = srl.FromXml<PropertyImplicitToCtorParam2>(xml);
   }

   [Fact]
   public void TuplesXml()
   {
      var easyTuple = new Tuple<string, long>("hello", 1337);
      var std = new DasSerializer();
      var xml = std.ToXml(easyTuple);
      var test2 = std.FromXml<Tuple<String, Int64>>(xml);
      //var vvv = JsonConvert.SerializeObject(easyTuple);

      Assert.True(easyTuple.Item1 == test2.Item1 &&
                  easyTuple.Item2 == test2.Item2);
   }

   [Fact]
   public void VersionXml()
   {
      var v = VersionContainer.TestInstance;
      var std = new DasSerializer();
      var xml = std.ToXml(v);

      var v2 = std.FromXml<VersionContainer>(xml);

      var areEqual = SlowEquality.AreEqual(v, v2);

      Assert.True(areEqual);
   }

   [Fact]
   public void XmlAsString()
   {
      var sc = SimpleClassObjectProperty.GetNullPayload();
      sc.Name = HttpUtility.HtmlDecode(
         @"&lt;DeployedStat Stat=&apos;Simple|PreFlop||*B3*|&apos; DisplayMode=&apos;Default&apos; Precision=&apos;1&apos; CacheIndex=&apos;341&apos; IsVersusHero=&apos;False&apos; Color=&apos;#000000&apos; MinimumSample=&apos;50&apos; ReportHeaderLabel=&apos;&apos;  Prefix=&apos;&apos; IsNegation=&apos;False&apos;&gt;
&lt;Filters&gt;
&lt;/Filters&gt;
&lt;/DeployedStat&gt;");
      sc.Payload = sc.Name;

      {
                
         var xml = srl.ToXml(sc);

         var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
         var badProp = "";
         Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
      }
   }

   [Fact]
   public void XmlPartialPropertyCtor()
   {
      var data = new PartialPropertyCtor(true, "string1", "string2",
         false, "string3", "string4", true, "string5", "string6",
         3.14, 11280, false, true, "string7", "string9", 0.15926);

      var xml = srl.ToXml(data);

      var data2 = srl.FromXml<PartialPropertyCtor>(xml);

      Assert.True(SlowEquality.AreEqual(data, data2));
   }
}