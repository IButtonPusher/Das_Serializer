using Das.Serializer;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;
#pragma warning disable 8602

// ReSharper disable All

namespace Serializer.Tests.Xml
{	
	public class XmlTests : TestBase
	{
		////[TestCategory("primitive"), TestCategory("xml"), TestMethod]
		[Fact]
		public void IntExplicitXml()
        {
			var someInt = 55;
			var srl = new DasSerializer();
			var xml = srl.ToXml(someInt);

			var int2 = srl.FromXml<Int32>(xml);
			Assert.True(someInt == int2);
		}

		////[TestCategory("primitive"), TestCategory("xml"), TestMethod]
		[Fact]
		public void Int32asInt16Xml()
		{
			var someInt = 55;
			
			var srl = new DasSerializer();
			srl.Settings.TypeSpecificity = TypeSpecificity.All;
			var xml = srl.ToXml<Int16>(someInt);
			////Debug.WriteLine("xml = " + xml);
			var int2 = srl.FromXml<Int16>(xml);

			var int3 = srl.FromXml<Int32>(xml);

			var int4 = (Int16)srl.FromXml(xml);

			Assert.True(someInt == int2 && int2 == int3 && int2 == int4);
		}

        [Fact]
		public void PrimitivePropertiesXml()
		{
			var sc = SimpleClassObjectProperty.GetNullPayload();

			var srl = new DasSerializer();
			var xml = srl.ToXml(sc);

			var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
			var badProp = "";
			Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));


            //var srl2 = new DasCoreSerializer();
            //var xml2 = srl2.ToXml(sc);

            //var sc3 = srl2.FromXml<SimpleClassObjectProperty>(xml);
            
            //Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
		}

        [Fact]
		public void XmlAsString()
		{
			var sc = SimpleClassObjectProperty.GetNullPayload();
			sc.Name = System.Web.HttpUtility.HtmlDecode(@"&lt;DeployedStat Stat=&apos;Simple|PreFlop||*B3*|&apos; DisplayMode=&apos;Default&apos; Precision=&apos;1&apos; CacheIndex=&apos;341&apos; IsVersusHero=&apos;False&apos; Color=&apos;#000000&apos; MinimumSample=&apos;50&apos; ReportHeaderLabel=&apos;&apos;  Prefix=&apos;&apos; IsNegation=&apos;False&apos;&gt;
&lt;Filters&gt;
&lt;/Filters&gt;
&lt;/DeployedStat&gt;");
			sc.Payload = sc.Name;

            {
                var srl = new DasSerializer();
                var xml = srl.ToXml(sc);

                var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
                var badProp = "";
                Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
            }

            //{
            //    var srl = new DasCoreSerializer();
            //    var xml = srl.ToXml(sc);

            //    var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
            //    var badProp = "";
            //    Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
            //}
        }

        [Fact]
        public void NoEmptyConstructorNullValueTypeValue()
        {
            var dto = new ArticleDto(1, "bob", "low", 2, null, 12344, "hello", -500);
            //var srl = Serializer;
            var srl = new DasSerializer();
            var xml = srl.ToXml(dto);
            var dto2 = srl.FromXml<ArticleDto>(xml);
            
            Assert.True(SlowEquality.AreEqual(dto, dto2));
        }

        [Fact]
        public void HeadlineCollection()
        {
            var fi = new FileInfo(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Xml",
                "Headlines.txt"));

            var xml = File.ReadAllText(fi.FullName);
            var srl = new DasSerializer();
            var count = 0;

            foreach (var a in srl.FromXmlItems<ValueArticleDto>(xml))
            {
                count++;
            }

			Assert.True(count == 100);
        }

        [Fact]
		public void ObjectReferenceTypeXml()
		{
			var sc = SimpleClassObjectProperty.GetNullPayload();
			sc.Payload = SimpleClassObjectProperty.GetPrimitivePayload();

            //{
            //    var srl = new DasCoreSerializer();
            //    var xml = srl.ToXml(sc);

            //    var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
            //    var badProp = "";
            //    Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
            //}

            {
                var srl = new DasSerializer();
                var xml = srl.ToXml(sc);

                var sc2 = srl.FromXml<SimpleClassObjectProperty>(xml);
                var badProp = "";
                Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
            }

           
        }

        [Fact]
		public void ObjectPropertiesXml()
		{
			var test = TestCompositeClass.Init();

			var srl = new DasSerializer();
			var xml = srl.ToXml(test);

			var sc2 = srl.FromXml<TestCompositeClass>(xml);
			var badProp = "";
			Assert.True(SlowEquality.AreEqual(test, sc2, ref badProp));
		}

        [Fact]
		public void CircularReferencesXml()
		{
			var sc1 = Teacher.Get();
			var srl = new DasSerializer();

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


		//[TestCategory("array"), TestCategory("xml"), TestCategory("collections"), TestMethod]
		[Fact]
		public void ClassWithPrimitiveArrayXml()
		{
			var mc1 = PrimitiveArray.Get();
			
			var xml = Serializer.ToXml(mc1);
			var res = Serializer.FromXml<PrimitiveArray>(xml);
			Assert.True(mc1.StringArray.SequenceEqual(res.StringArray));
		}

		//[TestCategory("array"), TestCategory("xml"), TestCategory("collections"), TestMethod]
		[Fact]
		public void ClassWithObjectArrayXml()
		{
			var arr1 = ObjectArray.Get();

			var xml = Serializer.ToXml(arr1);
			var res = Serializer.FromXml<ObjectArray>(xml);			

			Assert.True(arr1.ItemArray[0].Equals(res.ItemArray[0]));
			Assert.True(arr1.ItemArray[1].Equals(res.ItemArray[1]));
			
		}

		//[TestCategory("fallback"), TestCategory("xml"), TestMethod]
		[Fact]
		public void TimespanXml()
		{
			var dyn = new
			{
				//StartedAt = new TimeSpan(12, 34, 56)
				StartedAt = new DateTime(2000,1,1)
			};
			//var ts = new TimeSpan(12, 34, 56);
			var srl = new DasSerializer();
			var xml = srl.ToXml(dyn);

			var res = srl.FromXml(xml);
			//Assert.True(dyn.StartedAt == int2);
		}

		//[TestCategory("list"), TestCategory("xml"), TestCategory("collections"), TestMethod]
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

		//[TestCategory("list"), TestCategory("xml"), TestCategory("collections"), TestMethod]
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

		//[TestCategory("queue"), TestCategory("xml"), TestCategory("collections"), TestMethod]
		[Fact]
		public void QueuesXml()
		{
			var qs = new Queue<SimpleClassObjectProperty>();
			qs.Enqueue(SimpleClassObjectProperty.GetPrimitivePayload());

			var x = Serializer.ToXml(qs);
			var qs2 = Serializer.FromXml<Queue<SimpleClassObjectProperty>>(x);

			var qc = new ConcurrentQueue<SimpleClassObjectProperty>();
			qc.Enqueue(SimpleClassObjectProperty.GetNullPayload());

			x = Serializer.ToXml(qc);
			var qc2 = Serializer.FromXml<ConcurrentQueue<SimpleClassObjectProperty>>(x);

		}

		//[TestCategory("xml"), TestCategory("special"), TestMethod]
		[Fact]
		public void GdiColorInferredXml()
		{
			Serializer.Settings.NotFoundBehavior = TypeNotFound.ThrowException;

			var clr = Color.Purple;
			Serializer.Settings.TypeSpecificity = TypeSpecificity.All;
			var xxx = Serializer.ToXml(clr);
            var diesel = Serializer.FromXml(xxx);
            var yeti = (Color)diesel;
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
            var user3 = Serializer.FromXmlEx<User>(xml);

			var res = SlowEquality.AreEqual(user, user2) && 
                      SlowEquality.AreEqual(user3, user2);
			Assert.True(res);
        }


        private DasSerializer GetTypeSpecifyingSerializer()
        {
            return new DasSerializer
                (
                new DasSettings
                {
                    TypeSpecificity = TypeSpecificity.All
                });
        }

		//[TestCategory("dictionary"), TestCategory("xml"), TestMethod]
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
		}

		//[TestCategory("dictionary"), TestCategory("xml"), TestMethod]
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

		//[TestCategory("dictionary"), TestCategory("xml"), TestMethod]
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
			var v = new Version(3, 1, 4);
			var std = new DasSerializer();
			var xml = std.ToXml(v);
			
			var v2 = std.FromXml<Version>(xml);
			

			Assert.True(v == v2);
		}

        [Fact]
		public void AnonymousTypeXml()
		{
            var vvq = GetAnonymousObject();

			var srl = new DasSerializer();
			var xml = srl.ToXml(vvq);
			var res = srl.FromXml(xml);

            var isOk = SlowEquality.AreEqual(res, vvq);
            Assert.True(isOk);
        }
	}
}
