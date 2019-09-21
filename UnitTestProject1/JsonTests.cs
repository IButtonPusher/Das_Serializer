﻿using Das;
using Das.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
// ReSharper disable All

namespace UnitTestProject1
{
	[TestClass]
	public class JsonTests : TestBase
	{
		

		[TestCategory("primitive"), TestCategory("json"), TestMethod]
		public void IntExplicitJson()
		{
			var someInt = 55;
			var srl = new DasSerializer();
			var json = srl.ToJson(someInt);		

			var int2 = srl.FromJson<Int32>(json);
			Assert.IsTrue(someInt == int2);
		}

		

		[TestCategory("primitive"), TestCategory("json"), TestMethod]
		public void Int32asInt16Json()
		{
			var someInt = 55;
			
			var srl = new DasSerializer();
			srl.Settings.TypeSpecificity = TypeSpecificity.All;
			var json = srl.ToJson<Int16>(someInt);
			////Debug.WriteLine("json = " + json);
			var int2 = srl.FromJson<Int16>(json);

			var int3 = srl.FromJson<Int32>(json);

			var int4 = (Int16)srl.FromJson(json);

			Assert.IsTrue(someInt == int2 && int2 == int3 && int2 == int4);
		}



		[TestCategory("object"), TestCategory("json"), TestMethod]
		public void PrimitivePropertiesJson()
		{
			var sc = SimpleClass.GetNullPayload();
			
			var srl = new DasSerializer();
			var json = srl.ToJson(sc);

			var sc2 = srl.FromJson<SimpleClass>(json);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(sc,sc2, ref badProp));
		}

		[TestCategory("object"), TestCategory("json"), TestMethod]
		public void ExcludeDefaultValuesJson()
		{
			var sc = SimpleClass.GetNullPayload();
			sc.ID = 0;
			var srl = new DasSerializer();
			srl.Settings.IsOmitDefaultValues = true;
			var json = srl.ToJson(sc);

			var sc2 = srl.FromJson<SimpleClass>(json);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(sc, sc2, ref badProp));
		}


		[TestCategory("object"), TestCategory("json"), TestMethod]
		public void ReadOnlyPropertiesJson()
		{
			var sc = new SimpleClass("to everyone");
			sc.ID = 0;
			var srl = new DasSerializer();
			srl.Settings.IsOmitDefaultValues = true;
			srl.Settings.SerializationDepth |= SerializationDepth.GetOnlyProperties;
			var json = srl.ToJson(sc);

			var sc2 = srl.FromJson<SimpleClass>(json);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(sc, sc2, ref badProp));
		}

		[TestCategory("object"), TestCategory("json"), TestMethod]
		public void ObjectPropertiesJson()
		{
			var test = TestCompositeClass.Init();			

			var srl = new DasSerializer();
			var json = srl.ToJson(test);

			var sc2 = srl.FromJson<TestCompositeClass>(json);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(test, sc2, ref badProp));
		}

		[TestCategory("object"), TestCategory("json"), TestMethod]
		public void CircularReferencesjson()
		{
			var test = Teacher.Get();
			var srl = new DasSerializer();


			//restore the reference
			srl.Settings.CircularReferenceBehavior = CircularReference.SerializePath;
			var json = srl.ToJson(test);
			var sc2 = srl.FromJson<Teacher>(json);
			Assert.IsTrue(test.FirstName == sc2.FirstName && test.Pupils.First().MiddleName ==
				sc2.Pupils.First().MiddleName && test.Pupils.First().MathTeacher.FirstName ==
				sc2.Pupils.First().MathTeacher.FirstName);


			//lose the reference
			srl.Settings.CircularReferenceBehavior = CircularReference.IgnoreObject;
			json = srl.ToJson(test);
			sc2 = srl.FromJson<Teacher>(json);
			var badProp = "";
			Assert.IsTrue(test.FirstName == sc2.FirstName && test.Pupils.First().MiddleName ==
				sc2.Pupils.First().MiddleName && sc2.Pupils.First().MathTeacher == null);

			sc2 = srl.FromJson<Teacher>(json);

			Assert.IsFalse(SlowEquality.AreEqual(test, sc2, ref badProp));



			//fail
			srl.Settings.CircularReferenceBehavior = CircularReference.ThrowException;
			try
			{
				srl.ToJson(test);
				Assert.IsTrue(false);
			}
			catch { }
		}

		[TestCategory("array"), TestCategory("json"), TestCategory("collections"), TestMethod]
		public void ClassWithPrimitiveArrayJson()
		{
			var mc1 = PrimitiveArray.Get();
			
			var json = Serializer.ToJson(mc1);
			var res = Serializer.FromJson<PrimitiveArray>(json);			
			Assert.IsTrue(mc1.StringArray.SequenceEqual(res.StringArray));
			
		}

		[TestCategory("array"), TestCategory("json"), TestCategory("collections"), TestMethod]
		public void ClassWithObjectArrayJson()
		{
			var arr1 = ObjectArray.Get();

			var json = Serializer.ToJson(arr1);
			var res = Serializer.FromJson<ObjectArray>(json);
			

			Assert.IsTrue(arr1.ItemArray[0].Equals(res.ItemArray[0]));
			Assert.IsTrue(arr1.ItemArray[1].Equals(res.ItemArray[1]));
		}

		[TestCategory("list"), TestCategory("json"), TestCategory("collections"), TestMethod]
		public void ListsJson()
		{
			var list1 = ObjectList.Get();

			var json = Serializer.ToJson(list1);
			var res = Serializer.FromJson<ObjectList>(json);			

			for (var i = 0; i < list1.ItemList.Count; i++)
			{
				Assert.IsTrue(list1.ItemList[i].Equals(res.ItemList[i]));
			}

			////////////

			var list2 = PrimitiveList.Get();

			json = Serializer.ToJson(list2);
			var dres = Serializer.FromJson<PrimitiveList>(json);

			for (var i = 0; i < list2.DecimalList.Count; i++)
			{
				Assert.IsTrue(list2.DecimalList[i].Equals(dres.DecimalList[i]));
			}
		}

		[TestCategory("list"), TestCategory("json"), TestCategory("collections"), TestMethod]
		public void BlockingJson()
		{
			var bc = new BlockingCollection<SimpleClass>();
			bc.Add(SimpleClass.GetPrimitivePayload());
			bc.Add(SimpleClass.GetNullPayload());

			var json = Serializer.ToJson(bc);
			var res = Serializer.FromJson<BlockingCollection<SimpleClass>>(json);

			for (var i = 0; i < bc.Count; i++)
			{
                var left = bc.Skip(i).First();
                var right = res.Skip(i).First();

                Assert.IsTrue(left.Equals(right));
			}
		}

		[TestCategory("queue"), TestCategory("json"), TestCategory("collections"), TestMethod]
		public void QueuesJson()
		{
			var qs = new Queue<SimpleClass>();
			qs.Enqueue(SimpleClass.GetPrimitivePayload());

			var js = Serializer.ToJson(qs);
			var qs2 = Serializer.FromJson<Queue<SimpleClass>>(js);

			var qc = new ConcurrentQueue<SimpleClass>();
			qc.Enqueue(SimpleClass.GetNullPayload());

			js = Serializer.ToJson(qc);
			var qc2 = Serializer.FromJson<ConcurrentQueue<SimpleClass>>(js);

		}


		

		[TestCategory("json"), TestCategory("special"), TestMethod]
		public void TuplesJson()
		{
			var easyTuple = new Tuple<string, long>("hello", 1337);
			var std = new DasSerializer();
			var json = std.ToJson(easyTuple);
			var test2 = std.FromJson<Tuple<String, Int64>>(json);
			//var vvv = JsonConvert.SerializeObject(easyTuple);

			Assert.IsTrue(easyTuple.Item1 == test2.Item1 &&
				easyTuple.Item2 == test2.Item2);
		}

		//[TestCategory("json"), TestMethod]
		//public void GdiColorInferredJson()
		//{
		//	Color clr = Color.Purple;
		//	var srl = GetTypeSpecifyingSerializer();

		//	var xxx = srl.ToJson(clr);			

		//	Color yeti = (Color)Serializer.FromJson(xxx);
		//	Assert.AreEqual(clr, yeti);
		//}

		[TestCategory("json"), TestCategory("special"), TestMethod]
		public void GdiColorExplicitJson()
		{
			var clr = Color.Purple;
			var srl = new DasSerializer();
			
			var xxx = srl.ToJson(clr);
			//var jj = JsonConvert.SerializeObject(clr);
			var yeti = Serializer.FromJson<Color>(xxx);
			Assert.AreEqual(clr, yeti);
		}

		//[TestCategory("json"), TestMethod]
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
		//	Assert.IsTrue(sc.Equals(test2));
		//}

		[TestCategory("json"), TestMethod]
		public void ObjectPayloadKnownType()
		{
			var sc = new SimpleClass
			{
				ID = 4,
				Name = "bob",
				GPA = 3.14M,
				Payload = true
			};

			var std = new DasSerializer();
			var json = std.ToJson(sc);
			var test2 = std.FromJson<SimpleClass>(json);
			Assert.IsTrue(sc.Equals(test2));
		}		

		[TestCategory("dictionary"), TestCategory("json"), TestMethod]
		public void ClassWithDictionaryJson()
		{
			var mc1 = ObjectDictionary.Get();

			var json = Serializer.ToJson(mc1);

			var res = Serializer.FromJson<ObjectDictionary>(json);

			if (mc1 == null || res == null)
				Assert.IsFalse(true);
			if (mc1.Dic.Count != res.Dic.Count)
				Assert.IsFalse(true);

            var jRes = Serializer.ToJson(res);

            Assert.IsTrue(jRes == json);

		}

		#region special

		

		#endregion

		private DasSerializer GetTypeSpecifyingSerializer()
		{
			return new DasSerializer(
			new DasSettings
				{
					TypeSpecificity = TypeSpecificity.All
				}
			);
		}

		

	}
}
