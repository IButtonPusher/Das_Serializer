using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Das.Extensions;
using Das.Serializer;

// ReSharper disable All

namespace Serializer.Tests
{
	[TestClass]
	public class BinaryTests : TestBase
	{
		#region easy

		[TestCategory("primitive"), TestCategory("binary"), TestMethod]
		public void IntExplicitBinary()
		{
			var someInt = 55;
			var srl = Serializer;
			var bytes = srl.ToBytes(someInt);

			var int2 = srl.FromBytes<Int32>(bytes);
			Assert.IsTrue(someInt == int2);
		}

		[TestCategory("primitive"), TestCategory("binary"), TestMethod]
		public void Int32asInt16Binary()
		{
			var someInt = 55;
			
			var srl = Serializer;
			srl.Settings.TypeSpecificity = TypeSpecificity.All;
			
			var bytes = srl.ToBytes<Int16>(someInt);
			var int2 = srl.FromBytes<Int16>(bytes);

			var int3 = srl.FromBytes<Int32>(bytes);

			var int4 = (Int16)srl.FromBytes(bytes);

			Assert.IsTrue(someInt == int2 && int2 == int3 && int2 == int4);
		}

        [TestCategory("primitive"), TestCategory("binary"), TestMethod]
        public void StringOnly()
        {
            var str = "Lorem Ipsum";

            var srl = Serializer;
            var bytes = srl.ToBytes(str);

            var str2 = srl.FromBytes<String>(bytes);

            Assert.AreEqual(str, str2);
        }

        [TestCategory("special"), TestCategory("binary"), TestMethod]
        public void VersionBinary()
        {
            var v = new Version(3, 1, 4);
            var std = Serializer;
            var b = std.ToBytes(v);

            var v2 = std.FromBytes<Version>(b);

            Assert.IsTrue(v == v2);
        }


        [TestCategory("object"), TestCategory("binary"), TestMethod]
		public void PrimitivePropertiesBinary()
		{
			var sc = new SimpleClass
			{
				GPA = 4.01M,
				ID = 43,
				Name = "bob",
				Animal = Animals.Sheep
			};

			var srl = Serializer;
			
			var bytes = srl.ToBytes(sc);

			var sc2 = srl.FromBytes<SimpleClass>(bytes);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(sc, sc2, ref badProp));
		}

		[TestCategory("object"), TestCategory("binary"), TestMethod]
		public void BoolAsObjectBinary()
		{
			var sc = new SimpleClass
			{
				GPA = 4.01M,
				ID = 43,
				Name = "bob",
				Animal = Animals.Sheep,
				Payload = true
			};

			var srl = Serializer;
			var bytes = srl.ToBytes(sc);

			var sc2 = srl.FromBytes<SimpleClass>(bytes);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(sc, sc2, ref badProp));
		}

		[TestCategory("object"), TestCategory("binary"), TestMethod]
		public void EnumAsObjectBinary()
		{
			var sc = new SimpleClass
			{
				GPA = 4.01M,
				ID = 43,
				Name = "bob",
				Animal = Animals.Sheep,
				Payload = Animals.Frog 
			};

			var srl = Serializer;
			var bytes = srl.ToBytes(sc);

			var sc2 = srl.FromBytes<SimpleClass>(bytes);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(sc, sc2, ref badProp));
		}

		[TestCategory("types"), TestCategory("binary"), TestMethod]
		public void BinaryTypesAsObjects()
		{
			var type = typeof(Int32);
			var srl = Serializer;
			
			var bytes = srl.ToBytes(type);
			var type2 = srl.FromBytes<Type>(bytes);

            var areEqual = SlowEquality.AreEqual(type, type2);
            Assert.IsTrue(areEqual);
		}


        [TestCategory("types"), TestCategory("binary"), TestMethod]
        public void BinaryTypesFallbackComplex()
        {
            var srl = Serializer;

            var sc = new SimpleClass
            {
                Animal = Animals.Frog,
                GPA = 2.1M,
                Payload = new Object[2] { new Type[] { typeof(String) }, new Object[0] }
            };


            var bytes = srl.ToBytes(sc);
            var sc2 = srl.FromBytes<SimpleClass>(bytes);

            var areEqual = SlowEquality.AreEqual(sc, sc2);
            Assert.IsTrue(areEqual);
        }

        [TestCategory("object"), TestCategory("binary"), TestMethod]
		public void ObjectPropertiesBinary()
		{
			var test = TestCompositeClass.Init();

			var srl = Serializer;
			var byteMe = srl.ToBytes(test);

			var sc2 = srl.FromBytes<TestCompositeClass>(byteMe);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(test, sc2, ref badProp));
		}

		[TestCategory("object"), TestCategory("binary"), TestMethod]
		public void DeferredEnumerableBinary()
		{
			var test = new DeferredProperty();

			var srl = Serializer;
			
			var byteMe = srl.ToBytes(test);

			var sc2 = srl.FromBytes<DeferredProperty>(byteMe);
			
			Assert.IsTrue((test.SimpleProperty as IEnumerable<ISimpleClass>).Count(c => c != null) ==
				(sc2.SimpleProperty as IEnumerable<ISimpleClass>).Count(c => c != null));
		}


		[TestCategory("object"), TestCategory("binary"), TestMethod]
		public void NullClassPropertyBinary()
		{
			var test = TestCompositeClass.Init();
			test.SimpleLeft = null;
			var srl = Serializer;
			var byteMe = srl.ToBytes(test);

			var sc2 = srl.FromBytes<TestCompositeClass>(byteMe);
			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(test, sc2, ref badProp));
		}


		#endregion

		#region collections

		[TestCategory("array"), TestCategory("binary"), TestCategory("collections"), TestMethod]
		public void PrimitiveArrayBinary()
		{
			var mc1 = PrimitiveArray.Get();

			var bytes = Serializer.ToBytes(mc1);
			var res = Serializer.FromBytes<PrimitiveArray>(bytes);
			Assert.IsTrue(mc1.StringArray.SequenceEqual(res.StringArray));

			var bList = new List<int> { 1, 2, 3 };
			bytes = Serializer.ToBytes(bList);
			var bList2 = Serializer.FromBytes<List<Int32>>(bytes);
		}

		[TestCategory("array"), TestCategory("binary"), TestCategory("collections"), TestMethod]
		public void ObjectArrayBinary()
		{
			var arr1 = ObjectArray.Get();

			var bytes = Serializer.ToBytes(arr1);
			var res = Serializer.FromBytes<ObjectArray>(bytes);
			

			Assert.IsTrue(arr1.ItemArray[0].Equals(res.ItemArray[0]));
			Assert.IsTrue(arr1.ItemArray[1].Equals(res.ItemArray[1]));
		}

		[TestCategory("integers"), TestCategory("binary"),  TestMethod]
		public void UIntTypes()
		{
			var t1 = new UTypes
			{
				SByte = -5,
				U16 = 44,
				U32 = 66,
				U64 = 199
			};

			var slzr = Serializer;
			var bytes = slzr.ToBytes(t1);

			var t1Out = slzr.FromBytes<UTypes>(bytes);
			var tProp = "";
			if (!SlowEquality.AreEqual(t1, t1Out, ref tProp))
				Assert.IsFalse(true);

			var t2 = new UTypes
			{
				SByte = 127,
				U16 = 440,
				U32 = 660,
				U64 = 1990
			};

			bytes = slzr.ToBytes(t2);

			var t2Out = slzr.FromBytes<UTypes>(bytes);
			
			if (!SlowEquality.AreEqual(t2, t2Out, ref tProp))
				Assert.IsFalse(true);
		}


		[TestCategory("list"), TestCategory("binary"), 
         TestCategory("collections"), TestMethod]
		public void ObjectListBinary()
		{
			var list1 = ObjectList.Get();

			var byt = Serializer.ToBytes(list1);
			var res = Serializer.FromBytes<ObjectList>(byt);

			for (var i = 0; i < list1.ItemList.Count; i++)
			{
				Assert.IsTrue(list1.ItemList[i].Equals(res.ItemList[i]));
			}		
		}

        [TestCategory("list"), TestCategory("binary"), 
         TestCategory("collections"), TestCategory("null"),
         TestMethod]
        public void NullListBinary()
        {
            var list1 = new ObjectList
            {
                ItemList = null
            };

            var byt = Serializer.ToBytes(list1);
            var res = Serializer.FromBytes<ObjectList>(byt);

           Assert.IsNull(res.ItemList);
        }

        [TestCategory("list"), TestCategory("binary"), TestCategory("collections"), TestMethod]
        public void PrimitiveListBinary()
        {
            var list2 = PrimitiveList.Get();

            var byt = Serializer.ToBytes(list2);
            var dres = Serializer.FromBytes<PrimitiveList>(byt);

            for (var i = 0; i < list2.DecimalList.Count; i++)
            {
                Assert.IsTrue(list2.DecimalList[i].Equals(dres.DecimalList[i]));
            }
        }

        [TestCategory("list"), TestCategory("binary"), TestCategory("collections"), TestMethod]
		public void BlockingBinary()
		{
			var bc = new BlockingCollection<SimpleClass>();
			bc.Add(SimpleClass.GetPrimitivePayload());
			bc.Add(SimpleClass.GetNullPayload());

			var b = Serializer.ToBytes(bc);
			var res = Serializer.FromBytes<BlockingCollection<SimpleClass>>(b);

			for (var i = 0; i < bc.Count; i++)
			{
				Assert.IsTrue(bc.Skip(i).First().Equals(res.Skip(i).First()));
			}
		}

		[TestCategory("queue"), TestCategory("binary"), TestCategory("collections"), TestMethod]
		public void QueuesBinary()
		{
			var qs = new Queue<SimpleClass>();
			qs.Enqueue(SimpleClass.GetPrimitivePayload());

			var b = Serializer.ToBytes(qs);
			var qs2 = Serializer.FromBytes<Queue<SimpleClass>>(b);

			var qc = new ConcurrentQueue<SimpleClass>();
			qc.Enqueue(SimpleClass.GetNullPayload());

			b = Serializer.ToBytes(qc);
			var qc2 = Serializer.FromBytes<ConcurrentQueue<SimpleClass>>(b);

		}

		[TestCategory("dictionary"), TestCategory("binary"), TestMethod]
		public void DictionaryBinary()
		{
			var mc1 = ObjectDictionary.Get();
			var byt = Serializer.ToBytes(mc1);

			var mc2 = Serializer.FromBytes<ObjectDictionary>(byt);

			if (mc1 == null || mc2 == null)
				Assert.IsFalse(true);
			if (mc1.Dic.Count != mc2.Dic.Count)
				Assert.IsFalse(true);
			Assert.IsTrue(Serializer.ToBytes(mc2).Length == byt.Length);
		}

		#endregion


		[TestCategory("object"), TestCategory("binary"), TestMethod]
		public void CircularReferencesBytes()
		{
			var sc1 = Teacher.Get();
			var srl = new DasSerializer();


			//restore the reference
			srl.Settings.CircularReferenceBehavior = CircularReference.SerializePath;
			var Bytes = srl.ToBytes(sc1);
			var sc2 = srl.FromBytes<Teacher>(Bytes);

            var sc1FirstPupil = sc1.Pupils.First();
            var sc2FirstPupil = sc2.Pupils.First();

            Assert.IsTrue(sc1.FirstName == sc2.FirstName && sc1FirstPupil.MiddleName ==
                          sc2FirstPupil.MiddleName && sc1FirstPupil.MathTeacher.FirstName ==
                sc2FirstPupil.MathTeacher.FirstName);


			//lose the reference
			srl.Settings.CircularReferenceBehavior = CircularReference.IgnoreObject;
			Bytes = srl.ToBytes(sc1);
			sc2 = srl.FromBytes<Teacher>(Bytes);
			var badProp = "";

            sc1FirstPupil = sc1.Pupils.First();
            sc2FirstPupil = sc2.Pupils.First();

            Assert.IsTrue(sc1.FirstName == sc2.FirstName && sc1FirstPupil.MiddleName ==
                          sc2FirstPupil.MiddleName && sc2FirstPupil.MathTeacher == null);

			sc2 = srl.FromBytes<Teacher>(Bytes);

			Assert.IsFalse(SlowEquality.AreEqual(sc1, sc2, ref badProp));



			//fail
			srl.Settings.CircularReferenceBehavior = CircularReference.ThrowException;
			try
			{
				srl.ToBytes(sc1);
				Assert.IsTrue(false);
			}
			catch { }
		}


		[TestCategory("types"), TestCategory("binary"), TestMethod]
		public void InterfacesBinary()
		{
			var cont = new SimpleContainer
			{
				SimpleExample = SimpleClass.GetPrimitivePayload()
			};
            var std = Serializer;
			var bytes = std.ToBytes(cont);
			var cnt2 = std.FromBytes<SimpleContainer>(bytes);

			var classes = new List<ISimpleClass>();
			classes.Add(SimpleClass.GetNullPayload());
			classes.Add(SimpleClass.GetPrimitivePayload());
			
			bytes = std.ToBytes(classes);
            Serializer.SetTypeSurrogate(typeof(SimpleClass), typeof(SimpleClass2));
			var res = std.FromBytes< List<ISimpleClass>>(bytes);
		}

		[TestCategory("types"), TestCategory("binary"), TestMethod]
		public void SerializeAttributeBinary()
		{
			var waffle = new TestCompositeClass2();
			waffle.SimpleLeft = SimpleClass.GetPrimitivePayload();
			waffle.SimpleRight = SimpleClass.GetNullPayload();

			var std = Serializer;
			var bytes = std.ToBytes(waffle);
			var test2 = std.FromBytes<AbstractComposite>(bytes);

			var badProp = "";
			Assert.IsTrue(SlowEquality.AreEqual(waffle, test2, ref badProp));
		}

		#region special

		[TestCategory("special"), TestCategory("binary"), TestMethod]
		public void TuplesBinary()
		{
			var easyTuple = new Tuple<string, long>("hello", 1337);
			var std = Serializer;
			var bytes = std.ToBytes(easyTuple);
			var test2 = std.FromBytes<Tuple<String, Int64>>(bytes);

			Assert.IsTrue(easyTuple.Item1 == test2.Item1 &&
				easyTuple.Item2 == test2.Item2);
		}

		[TestCategory("special"), TestCategory("binary"), TestMethod]
		public void NullableTuplesBinary()
		{
			var t1 = new Tuple<decimal?, decimal?, Color>(null, 100, Color.FromArgb(22, 33, 44));
			var std = Serializer;
			var bytes = std.ToBytes(t1);
			var t2 = std.FromBytes<Tuple<decimal?, decimal?, Color>>(bytes);

			Assert.IsTrue(t1.Item1 == t2.Item1 &&
				t1.Item2 == t2.Item2 && t1.Item3 == t2.Item3);
		}


		[TestCategory("object"), TestCategory("binary"), TestMethod]
		public void EventAsObjectPropBinary()
		{
			var sc = new SimpleClass
			{
				Payload = new EventArgs(), GPA = 5.0M, Animal = Animals.Frog, ID =77 
			};

			var srl = Serializer;
			var bytes = srl.ToBytes(sc);
			var sc2 = srl.FromBytes<SimpleClass>(bytes);
			
			Assert.IsTrue(sc2.Payload.GetType() == typeof(EventArgs));
			Assert.IsTrue(sc.GPA == sc2.GPA && sc.Animal == sc2.Animal && sc.ID == sc2.ID);			
		}


		

		[TestCategory("binary"), TestCategory("special"), TestMethod]
		public void GdiColorExplicitBinary()
		{
			var clr = Color.Purple;
			var srl = Serializer;

			var xxx = srl.ToBytes(clr);
			var yeti = Serializer.FromBytes<Color>(xxx);
			Assert.AreEqual(clr, yeti);
		}

		[TestCategory("binary"), TestCategory("special"), TestMethod]
		public void GdiPointBinary()
		{
			var pt = new Point(1, 0);
            pt.Y = 1;
            pt.Y = 0;
            pt.TrySetPropertyValue(nameof(Point.X), 2);
			
			var srl = Serializer;

			var xxx = srl.ToBytes(pt);
			var yeti = Serializer.FromBytes<Point>(xxx);
            Trace.WriteLine("over");
			Assert.AreEqual(pt, yeti);
		}

		#endregion

	}
}
