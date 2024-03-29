﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Das.Serializer;
using Newtonsoft.Json;
using Serializer.Tests.ProtocolBuffers;
using Xunit;

//using Newtonsoft.Json;

// ReSharper disable All

namespace Serializer.Tests.Json;

public class JsonTests : TestBase
{
   private DasSerializer GetTypeSpecifyingSerializer()
   {
      return new DasSerializer(
         new DasSettings
         {
            TypeSpecificity = TypeSpecificity.All
         }
      );
   }

   [Fact]
   public void AnonymousTypeJson()
   {
      var vvq = GetAnonymousObject();

      var srl = new DasSerializer();
      var json = srl.ToJson(vvq);
      var res = srl.FromJson(json);

      var isOk = SlowEquality.AreEqual(res, vvq);
      Assert.True(isOk);
   }

   [Fact]
   public void BlockingJson()
   {
      var bc = new BlockingCollection<SimpleClassObjectProperty>();
      bc.Add(SimpleClassObjectProperty.GetPrimitivePayload());
      bc.Add(SimpleClassObjectProperty.GetNullPayload());

      var json = Serializer.ToJson(bc);
      var res = Serializer.FromJson<BlockingCollection<SimpleClassObjectProperty>>(json);

      for (var i = 0; i < bc.Count; i++)
      {
         var left = bc.Skip(i).First();
         var right = res.Skip(i).First();

         Assert.True(left.Equals(right));
      }
   }

   [Fact]
   public void CircularReferencesJson()
   {
      var test = Teacher.Get();
      var srl = new DasSerializer();


      //restore the reference
      srl.Settings.CircularReferenceBehavior = CircularReference.SerializePath;
      var json = srl.ToJson(test);
      var sc2 = srl.FromJson<Teacher>(json);
      Assert.True(test.FirstName == sc2.FirstName && test.Pupils.First().MiddleName ==
         sc2.Pupils.First().MiddleName && test.Pupils.First().MathTeacher.FirstName ==
         sc2.Pupils.First().MathTeacher.FirstName);


      //lose the reference
      srl.Settings.CircularReferenceBehavior = CircularReference.IgnoreObject;
      json = srl.ToJson(test);
      sc2 = srl.FromJson<Teacher>(json);
      var badProp = "";
      Assert.True(test.FirstName == sc2.FirstName && test.Pupils.First().MiddleName ==
         sc2.Pupils.First().MiddleName && sc2.Pupils.First().MathTeacher == null);

      sc2 = srl.FromJson<Teacher>(json);

      Assert.False(SlowEquality.AreEqual(test, sc2, ref badProp));


      //fail
      srl.Settings.CircularReferenceBehavior = CircularReference.ThrowException;
      try
      {
         srl.ToJson(test);
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
   public void ClassWithDictionaryJson()
   {
      var mc1 = DictionaryPropertyMessage.DefaultValue;
      var json = Serializer.ToJson(mc1);
      var json2 = Serializer.ToJsonEx(mc1);
      var json3 = JsonConvert.SerializeObject(mc1);
            
      var res1 = Serializer.FromJson<DictionaryPropertyMessage>(json);
      var res2 = Serializer.FromJson<DictionaryPropertyMessage>(json);
      var res3 = JsonConvert.DeserializeObject<DictionaryPropertyMessage>(json3);

      Assert.True(SlowEquality.AreEqual(mc1, res1));
      Assert.True(SlowEquality.AreEqual(mc1, res2));

   }

   [Fact]
   public void ClassWithObjectDictionaryJson()
   {
      var mc1 = ObjectDictionary.Get();

      var json = Serializer.ToJson(mc1);
      //var json2 = Serializer.ToJsonEx(mc1);

      var res = Serializer.FromJson<ObjectDictionary>(json);

      if (mc1 == null || res == null)
         Assert.False(true);
      else if (mc1.Dic.Count != res.Dic.Count)
         Assert.False(true);

            
      var jRes = Serializer.ToJson(res);

      Assert.True(jRes == json);
   }

   [Fact]
   public void ClassWithObjectArrayJson()
   {
      var arr1 = ObjectArray.Get();

      var json = Serializer.ToJson(arr1);
      var res = Serializer.FromJson<ObjectArray>(json);


      Assert.True(arr1.ItemArray[0].Equals(res.ItemArray[0]));
      Assert.True(arr1.ItemArray[1].Equals(res.ItemArray[1]));
   }

   [Fact]
   public void ClassWithPrimitiveArrayJson()
   {
      var mc1 = PrimitiveArray.Get();

      var json = Serializer.ToJson(mc1);
      var res = Serializer.FromJson<PrimitiveArray>(json);
      Assert.True(mc1.StringArray.SequenceEqual(res.StringArray));

      mc1.StringArray = new String[0];
      json = Serializer.ToJson(mc1);
      res = Serializer.FromJson<PrimitiveArray>(json);
      Assert.True(mc1.StringArray.SequenceEqual(res.StringArray));
   }


   [Fact]
   public void EmptyStringIsNotNull()
   {
      var eg = SimpleClass.GetExample<SimpleClass>();
      eg.Name = string.Empty;

      var json = Serializer.ToJson(eg);
      var eg2 = Serializer.FromJson<SimpleClass>(json);

      Assert.NotNull(eg2.Name);
   }


   [Fact]
   public void ExcludeDefaultValuesJson()
   {
      var sc = SimpleClassObjectProperty.GetNullPayload();
      sc.ID = 0;
      var settings = DasSettings.CloneDefault();

      var srl = new DasSerializer(settings);
      settings.IsOmitDefaultValues = true;
      var json = srl.ToJson(sc);

      var sc2 = srl.FromJson<SimpleClassObjectProperty>(json);
      var badProp = "";
      Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
   }

   [Fact]
   public void ExplicitlyNullResult()
   {
      var json = "{\"id\":25,\"result\":null}";
      var res = Serializer.FromJson<ResponseIdTest>(json);

      var err = res.result?.error;
      var test = res.result?.error?.message;
   }


   [Fact]
   public void GdiColorExplicitJson()
   {
      var clr = Color.Purple;
      var srl = new DasSerializer();

      var obj = new
      {
         Color = clr
      };

      //var json = srl.ToJson(clr);
      //var yeti = Serializer.FromJson<Color>(json);
      //Assert.Equal(clr, yeti);

      var json = srl.ToJson(obj);
      var yeti = Serializer.FromJson(json, obj.GetType());
      Assert.Equal(obj, yeti);
   }

   //these are not legal json
   //[Fact]
   //public void GdiColorInferredJson()
   //{
   //    Serializer.Settings.TypeNotFoundBehavior = TypeNotFoundBehavior.ThrowException;

   //    var clr = Color.Purple;
   //    Serializer.Settings.TypeSpecificity = TypeSpecificity.All;
   //    var json = Serializer.ToJson(clr);
   //    var diesel = Serializer.FromJson(json);
   //    var yeti = (Color) diesel;
   //    Serializer.Settings.TypeSpecificity = TypeSpecificity.Discrepancy;
   //    Assert.True(clr.R == yeti.R && clr.G == yeti.G && clr.B == yeti.B);
   //}

   //[Fact]
   //public void Int32asInt16Json()
   //{
   //    var someInt = 55;

   //    var srl = new DasSerializer();
   //    srl.Settings.TypeSpecificity = TypeSpecificity.All;
   //    var json = srl.ToJson<Int16>(someInt);

   //    var int2 = srl.FromJson<Int16>(json);

   //    var int3 = srl.FromJson<Int32>(json);

   //    var int4 = (Int16) srl.FromJson(json);

   //    Assert.True(someInt == int2 && int2 == int3 && int2 == int4);
   //}

   //[Fact]
   //public void IntExplicitJson()
   //{
   //    var someInt = 55;
   //    var srl = new DasSerializer();
   //    var json = srl.ToJson(someInt);

   //    var int2 = srl.FromJson<Int32>(json);
   //    Assert.True(someInt == int2);
   //}

   [Fact]
   public void ListJson()
   {
      var bc = new List<SimpleClassObjectProperty>();
      bc.Add(SimpleClassObjectProperty.GetPrimitivePayload());
      bc.Add(SimpleClassObjectProperty.GetNullPayload());

      var json = Serializer.ToJson(bc);
      var json3 = JsonConvert.SerializeObject(bc);
      //var json2 = Serializer.ToJsonEx(bc);
      var res = Serializer.FromJson<List<SimpleClassObjectProperty>>(json);

      for (var i = 0; i < bc.Count; i++)
      {
         var left = bc.Skip(i).First();
         var right = res.Skip(i).First();

         Assert.True(left.Equals(right));
      }
   }


   [Fact]
   public void ListsJson()
   {
      var list1 = ObjectList.Get();

      var json = Serializer.ToJson(list1);
      var res = Serializer.FromJson<ObjectList>(json);

      for (var i = 0; i < list1.ItemList.Count; i++)
      {
         Assert.True(list1.ItemList[i].Equals(res.ItemList[i]));
      }

      ////////////

      var list2 = PrimitiveList.Get();

      json = Serializer.ToJson(list2);
      var dres = Serializer.FromJson<PrimitiveList>(json);

      for (var i = 0; i < list2.DecimalList.Count; i++)
      {
         Assert.True(list2.DecimalList[i].Equals(dres.DecimalList[i]));
      }
   }

   ////[TestCategory("json"), TestMethod]
   //public void ObjectPayloadJson()
   //{
   //	SimpleClass sc = new SimpleClass
   //	{
   //		ID = 4,
   //		Name = "bob",
   //		GPA = 3.14M,
   //		Payload = true
   //	};

   //	DasSerializer std = new DasSerializer();
   //	var json = std.ToJson(sc);
   //	//var jc = JsonConvert.SerializeObject(sc);
   //	Object test2 = std.FromJson(json);
   //	Assert.True(sc.Equals(test2));
   //}


   [Fact]
   public void MultiProperties()
   {
      var msg = MultiPropMessage.GetTestOne();
      var json = Serializer.ToJson(msg);
      var json2 = Serializer.ToJsonEx(msg);

      var res = Serializer.FromJson<MultiPropMessage>(json2);

      Assert.True(SlowEquality.AreEqual(msg, res));
   }

   [Fact]
   public void ObjectPayloadKnownType()
   {
      var sc = new SimpleClassObjectProperty
      {
         ID = 4,
         Name = "bob\\walters",
         GPA = 3.14M,
         Payload = true
      };

      var std = new DasSerializer();
      var json = std.ToJson(sc);
      var test2 = std.FromJson<SimpleClassObjectProperty>(json);
      Assert.True(sc.Equals(test2));
   }

   [Fact]
   public void ObjectPropertiesJson()
   {
      var test = TestCompositeClass.Init();

      var srl = new DasSerializer();
      var json = srl.ToJson(test);

      var sc2 = srl.FromJson<TestCompositeClass>(json);
      var badProp = "";
      var rolf = SlowEquality.AreEqual(test, sc2, ref badProp);

      Assert.True(rolf);
   }

   private static void TestPrint(String str)
   {}

   [Fact]
   public void DynamicPrintPrimitiveProperties()
   {
      var eg = SimpleClass.GetExample<SimpleClass>();

      if (eg.DateOfBirth.HasValue)
      {
         DerpObject(eg.ShiftPreference);
      }

      var rdrrFlex = new TimeSpanConverter().ConvertToInvariantString(eg.ShiftPreference);

            

      //  Derp(eg.ShiftPreference.Ticks);
      if (eg.DateOfBirth.HasValue)
      {
         var rdrr = eg.DateOfBirth.Value.ToString();
         if (rdrr == null)
            throw new Exception();
      }

      var json = Serializer.ToJsonEx(eg, Serializer.Settings);

      var json2 = JsonConvert.SerializeObject(eg);

      var eg2 = Serializer.FromJson<SimpleClass>(json);

      Assert.NotNull(eg2.Name);

      var badProp = "";
      var rolf = SlowEquality.AreEqual(eg, eg2, ref badProp);
      Assert.True(rolf);

      eg.DateOfBirth = null;
      json = Serializer.ToJsonEx(eg, Serializer.Settings);
      json2 = JsonConvert.SerializeObject(eg);
      eg2 = Serializer.FromJson<SimpleClass>(json);
      Assert.NotNull(eg2.Name);
        
      rolf = SlowEquality.AreEqual(eg, eg2, ref badProp);

      Assert.True(rolf);
   }

   private static void DerpObject(Object o)
   {

   }

   private static void Derp(Int32 dd)
   {

   }

   private static void Derp(Char dd)
   {

   }

   //private static void Derp(Int64 dd)
   //{

   //}

   [Fact]
   public void PrimitivePropertiesJson()
   {
      var sc = SimpleClassObjectProperty.GetNullPayload();

      var srl = new DasSerializer();
      var json = srl.ToJson(sc);

      var sc2 = srl.FromJson<SimpleClassObjectProperty>(json);
      var badProp = "";
      Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
   }


   [Fact]
   public void QueuesJson()
   {
      var qs = new Queue<SimpleClassObjectProperty>();
      qs.Enqueue(SimpleClassObjectProperty.GetPrimitivePayload());

      var js = Serializer.ToJson(qs);
      var qs2 = Serializer.FromJson<Queue<SimpleClassObjectProperty>>(js);

      var qc = new ConcurrentQueue<SimpleClassObjectProperty>();
      qc.Enqueue(SimpleClassObjectProperty.GetNullPayload());

      js = Serializer.ToJson(qc);
      var qc2 = Serializer.FromJson<ConcurrentQueue<SimpleClassObjectProperty>>(js);
   }

   [Fact]
   public void ReadOnlyPropertiesJson()
   {
      var sc = new SimpleClassObjectProperty("to everyone");
      sc.ID = 0;
      var settings = DasSettings.CloneDefault();
      settings.IsOmitDefaultValues = true;
      settings.SerializationDepth |= SerializationDepth.GetOnlyProperties;
      var srl = new DasSerializer(settings);

      var json = srl.ToJson(sc);

      var sc2 = srl.FromJson<SimpleClassObjectProperty>(json);
      var badProp = "";
      Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
   }

   [Fact]
   public void SpecifyRuntimeObject()
   {
      var vvq = GetAnonymousObject();

      var json = _serializer.ToJson(vvq);
      var res = _serializer.FromJson<RuntimeObject>(json);

      var isOk = Equals(res["Id"], _serializer.ObjectManipulator.GetPropertyValue(vvq, "Id", PropertyNameFormat.Default)) &&
                 Equals(res["Name"], _serializer.ObjectManipulator.GetPropertyValue(vvq, "Name", PropertyNameFormat.Default)) &&
                 Equals(res["NumericString"],
                    _serializer.ObjectManipulator.GetPropertyValue(vvq, "NumericString", PropertyNameFormat.Default)) &&
                 Equals(res["ZipCode"], _serializer.ObjectManipulator.GetPropertyValue(vvq, "ZipCode", PropertyNameFormat.Default));


      //var isOk = SlowEquality.AreEqual(res, vvq);
      Assert.True(isOk);
   }


   //[TestCategory("json"), TestCategory("special"), TestMethod]
   [Fact]
   public void TuplesJson()
   {
      var easyTuple = new Tuple<string, long>("hello", 1337);
      var std = new DasSerializer();
      var json = std.ToJson(easyTuple);
      var test2 = std.FromJson<Tuple<String, Int64>>(json);
      //var vvv = JsonConvert.SerializeObject(easyTuple);

      Assert.True(easyTuple.Item1 == test2.Item1 &&
                  easyTuple.Item2 == test2.Item2);
   }

   [Fact]
   public void VersionJson()
   {
      var v = VersionContainer.TestInstance;
      var std = new DasSerializer();
      var json = std.ToJson(v);

      var v2 = std.FromJson<VersionContainer>(json);

      var areEqual = SlowEquality.AreEqual(v, v2);

      Assert.True(areEqual);
   }
}