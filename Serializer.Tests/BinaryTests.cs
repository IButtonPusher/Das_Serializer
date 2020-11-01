using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Das.Extensions;
using Das.Serializer;
using Xunit;
#pragma warning disable 8602
#pragma warning disable 8625

// ReSharper disable All

namespace Serializer.Tests
{
    public class BinaryTests : TestBase
    {
        #region easy

        [Fact]
        public void IntExplicitBinary()
        {
            var someInt = 55;
            var srl = Serializer;
            var bytes = srl.ToBytes(someInt);

            var int2 = srl.FromBytes<Int32>(bytes);
            Assert.True(someInt == int2);
        }

        [Fact]
        public void Int32asInt16Binary()
        {
            var someInt = 55;

            var srl = Serializer;
            srl.Settings.TypeSpecificity = TypeSpecificity.All;

            var bytes = srl.ToBytes<Int16>(someInt);
            var int2 = srl.FromBytes<Int16>(bytes);

            var int3 = srl.FromBytes<Int32>(bytes);

            var int4 = (Int16) srl.FromBytes(bytes);

            Assert.True(someInt == int2 && int2 == int3 && int2 == int4);
        }

        [Fact]
        public void StringOnly()
        {
            var str = "Lorem Ipsum";

            var srl = Serializer;
            var bytes = srl.ToBytes(str);

            var str2 = srl.FromBytes<String>(bytes);

            Assert.Equal(str, str2);
        }

        [Fact]
        public void VersionBinary()
        {
            var v = new Version(3, 1, 4);
            var std = Serializer;
            var b = std.ToBytes(v);

            var v2 = std.FromBytes<Version>(b);

            Assert.True(v == v2);
        }

        [Fact]
        public void PrimitivePropertiesBinary()
        {
            var sc = new SimpleClassObjectProperty
            {
                GPA = 4.01M,
                ID = 43,
                Name = "bob",
                Animal = Animals.Sheep
            };

            var srl = Serializer;

            var bytes = srl.ToBytes(sc);

            var sc2 = srl.FromBytes<SimpleClassObjectProperty>(bytes);
            var badProp = "";
            Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
        }

        [Fact]
        public void BoolAsObjectBinary()
        {
            var sc = new SimpleClassObjectProperty
            {
                GPA = 4.01M,
                ID = 43,
                Name = "bob",
                Animal = Animals.Sheep,
                Payload = true
            };

            var srl = Serializer;
            var bytes = srl.ToBytes(sc);

            var sc2 = srl.FromBytes<SimpleClassObjectProperty>(bytes);
            var badProp = "";
            Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
        }

        [Fact]
        public void EnumAsObjectBinary()
        {
            var sc = new SimpleClassObjectProperty
            {
                GPA = 4.01M,
                ID = 43,
                Name = "bob",
                Animal = Animals.Sheep,
                Payload = Animals.Frog
            };

            var srl = Serializer;
            var bytes = srl.ToBytes(sc);

            var sc2 = srl.FromBytes<SimpleClassObjectProperty>(bytes);
            var badProp = "";
            Assert.True(SlowEquality.AreEqual(sc, sc2, ref badProp));
        }

        [Fact]
        public void BinaryTypesAsObjects()
        {
            var type = typeof(Int32);
            var srl = Serializer;

            var bytes = srl.ToBytes(type);
            var type2 = srl.FromBytes<Type>(bytes);

            var areEqual = SlowEquality.AreEqual(type, type2);
            Assert.True(areEqual);
        }

        [Fact]
        public void BinaryTypesFallbackComplex()
        {
            var srl = Serializer;

            var sc = new SimpleClassObjectProperty
            {
                Animal = Animals.Frog,
                GPA = 2.1M,
                Payload = new Object[2] {new Type[] {typeof(String)}, new Object[0]}
            };


            var bytes = srl.ToBytes(sc);
            var sc2 = srl.FromBytes<SimpleClassObjectProperty>(bytes);

            var areEqual = SlowEquality.AreEqual(sc, sc2);
            Assert.True(areEqual);
        }

        [Fact]
        public void ObjectPropertiesBinary()
        {
            var test = TestCompositeClass.Init();

            var srl = Serializer;
            var byteMe = srl.ToBytes(test);

            var sc2 = srl.FromBytes<TestCompositeClass>(byteMe);
            var badProp = "";
            Assert.True(SlowEquality.AreEqual(test, sc2, ref badProp));
        }

        [Fact]
        public void DeferredEnumerableBinary()
        {
            var test = new DeferredProperty();

            var srl = Serializer;

            var byteMe = srl.ToBytes(test);

            var sc2 = srl.FromBytes<DeferredProperty>(byteMe);

            Assert.True((test.SimpleProperty as IEnumerable<ISimpleClass>).Count(c => c != null) ==
                        (sc2.SimpleProperty as IEnumerable<ISimpleClass>).Count(c => c != null));
        }

        [Fact]
        public void NullClassPropertyBinary()
        {
            var test = TestCompositeClass.Init();
            test.SimpleLeft = null;
            var srl = Serializer;
            var byteMe = srl.ToBytes(test);

            var sc2 = srl.FromBytes<TestCompositeClass>(byteMe);
            var badProp = "";
            Assert.True(SlowEquality.AreEqual(test, sc2, ref badProp));
        }


        #endregion

        #region collections
        
        [Fact]
        public void PrimitiveArrayBinary()
        {
            var mc1 = PrimitiveArray.Get();

            var bytes = Serializer.ToBytes(mc1);
            var res = Serializer.FromBytes<PrimitiveArray>(bytes);
            Assert.True(mc1.StringArray.SequenceEqual(res.StringArray));

            var bList = new List<int> {1, 2, 3};
            bytes = Serializer.ToBytes(bList);
            var bList2 = Serializer.FromBytes<List<Int32>>(bytes);
        }

        [Fact]
        public void ObjectArrayBinary()
        {
            var arr1 = ObjectArray.Get();

            var bytes = Serializer.ToBytes(arr1);
            var res = Serializer.FromBytes<ObjectArray>(bytes);


            Assert.True(arr1.ItemArray[0].Equals(res.ItemArray[0]));
            Assert.True(arr1.ItemArray[1].Equals(res.ItemArray[1]));
        }

        [Fact]
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
                Assert.False(true);

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
                Assert.False(true);
        }

        [Fact]
        public void ObjectListBinary()
        {
            var list1 = ObjectList.Get();

            var byt = Serializer.ToBytes(list1);
            var res = Serializer.FromBytes<ObjectList>(byt);

            for (var i = 0; i < list1.ItemList.Count; i++)
            {
                Assert.True(list1.ItemList[i].Equals(res.ItemList[i]));
            }
        }

        [Fact]
        public void NullListBinary()
        {
            var list1 = new ObjectList
            {
                ItemList = null
            };

            var byt = Serializer.ToBytes(list1);
            var res = Serializer.FromBytes<ObjectList>(byt);

            Assert.Null(res.ItemList);
        }

        [Fact]
        public void PrimitiveListBinary()
        {
            var list2 = PrimitiveList.Get();

            var byt = Serializer.ToBytes(list2);
            var dres = Serializer.FromBytes<PrimitiveList>(byt);

            for (var i = 0; i < list2.DecimalList.Count; i++)
            {
                Assert.True(list2.DecimalList[i].Equals(dres.DecimalList[i]));
            }
        }

        [Fact]
        public void BlockingBinary()
        {
            var bc = new BlockingCollection<SimpleClassObjectProperty>();
            bc.Add(SimpleClassObjectProperty.GetPrimitivePayload());
            bc.Add(SimpleClassObjectProperty.GetNullPayload());

            var b = Serializer.ToBytes(bc);
            var res = Serializer.FromBytes<BlockingCollection<SimpleClassObjectProperty>>(b);

            for (var i = 0; i < bc.Count; i++)
            {
                Assert.True(bc.Skip(i).First().Equals(res.Skip(i).First()));
            }
        }

        [Fact]
        public void QueuesBinary()
        {
            var qs = new Queue<SimpleClassObjectProperty>();
            qs.Enqueue(SimpleClassObjectProperty.GetPrimitivePayload());

            var b = Serializer.ToBytes(qs);
            var qs2 = Serializer.FromBytes<Queue<SimpleClassObjectProperty>>(b);

            var qc = new ConcurrentQueue<SimpleClassObjectProperty>();
            qc.Enqueue(SimpleClassObjectProperty.GetNullPayload());

            b = Serializer.ToBytes(qc);
            var qc2 = Serializer.FromBytes<ConcurrentQueue<SimpleClassObjectProperty>>(b);

        }

        [Fact]
        public void DictionaryBinary()
        {
            var mc1 = ObjectDictionary.Get();
            var byt = Serializer.ToBytes(mc1);

            var mc2 = Serializer.FromBytes<ObjectDictionary>(byt);

            if (mc1 == null || mc2 == null)
                Assert.False(true);
            if (mc1.Dic.Count != mc2.Dic.Count)
                Assert.False(true);
            Assert.True(Serializer.ToBytes(mc2).Length == byt.Length);
        }

        #endregion

        [Fact]
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

            Assert.True(sc1.FirstName == sc2.FirstName && sc1FirstPupil.MiddleName ==
                sc2FirstPupil.MiddleName && sc1FirstPupil.MathTeacher.FirstName ==
                sc2FirstPupil.MathTeacher.FirstName);


            //lose the reference
            srl.Settings.CircularReferenceBehavior = CircularReference.IgnoreObject;
            Bytes = srl.ToBytes(sc1);
            sc2 = srl.FromBytes<Teacher>(Bytes);
            var badProp = "";

            sc1FirstPupil = sc1.Pupils.First();
            sc2FirstPupil = sc2.Pupils.First();

            Assert.True(sc1.FirstName == sc2.FirstName && sc1FirstPupil.MiddleName ==
                sc2FirstPupil.MiddleName && sc2FirstPupil.MathTeacher == null);

            sc2 = srl.FromBytes<Teacher>(Bytes);

            Assert.False(SlowEquality.AreEqual(sc1, sc2, ref badProp));



            //fail
            srl.Settings.CircularReferenceBehavior = CircularReference.ThrowException;
            try
            {
                srl.ToBytes(sc1);
                Assert.True(false);
            }
            catch
            {
            }
        }

        [Fact]
        public void InterfacesBinary()
        {
            var cont = new SimpleContainer
            {
                SimpleExample = SimpleClassObjectProperty.GetPrimitivePayload()
            };
            var std = Serializer;
            var bytes = std.ToBytes(cont);
            var cnt2 = std.FromBytes<SimpleContainer>(bytes);

            var classes = new List<ISimpleClass>();
            classes.Add(SimpleClassObjectProperty.GetNullPayload());
            classes.Add(SimpleClassObjectProperty.GetPrimitivePayload());

            bytes = std.ToBytes(classes);
            Serializer.SetTypeSurrogate(typeof(SimpleClassObjectProperty), typeof(SimpleClass2));
            var res = std.FromBytes<List<ISimpleClass>>(bytes);
        }

        [Fact]
        public void SerializeAttributeBinary()
        {
            var waffle = new TestCompositeClass2();
            waffle.SimpleLeft = SimpleClassObjectProperty.GetPrimitivePayload();
            waffle.SimpleRight = SimpleClassObjectProperty.GetNullPayload();

            var std = Serializer;
            var bytes = std.ToBytes(waffle);

            #if GENERATECODE

            var test2 = std.FromBytes<AbstractComposite>(bytes);
            #else
            var test2 = std.FromBytes<AbstractComposite>(bytes);
            #endif

            var badProp = "";
            var isOk = SlowEquality.AreEqual(waffle, test2, ref badProp);
            Assert.True(isOk);
        }

        #region special

        [Fact]
        public void TuplesBinary()
        {
            var easyTuple = new Tuple<string, long>("hello", 1337);
            var std = Serializer;
            var bytes = std.ToBytes(easyTuple);
            var test2 = std.FromBytes<Tuple<String, Int64>>(bytes);

            Assert.True(easyTuple.Item1 == test2.Item1 &&
                        easyTuple.Item2 == test2.Item2);
        }

        [Fact]
        public void NullableTuplesBinary()
        {
            var t1 = new Tuple<decimal?, decimal?, Color>(null, 100, Color.FromArgb(22, 33, 44));
            var std = Serializer;
            var bytes = std.ToBytes(t1);
            var t2 = std.FromBytes<Tuple<decimal?, decimal?, Color>>(bytes);

            Assert.True(t1.Item1 == t2.Item1 &&
                        t1.Item2 == t2.Item2 && t1.Item3 == t2.Item3);
        }

        [Fact]
        public void EventAsObjectPropBinary()
        {
            var sc = new SimpleClassObjectProperty
            {
                Payload = new EventArgs(), GPA = 5.0M, Animal = Animals.Frog, ID = 77
            };

            var srl = Serializer;
            var bytes = srl.ToBytes(sc);
            var sc2 = srl.FromBytes<SimpleClassObjectProperty>(bytes);

            Assert.True(sc2.Payload.GetType() == typeof(EventArgs));
            Assert.True(sc.GPA == sc2.GPA && sc.Animal == sc2.Animal && sc.ID == sc2.ID);
        }

        [Fact]
        public void GdiColorExplicitBinary()
        {
            var clr = Color.Purple;
            var srl = Serializer;

            var xxx = srl.ToBytes(clr);
            var yeti = Serializer.FromBytes<Color>(xxx);
            Assert.Equal(clr, yeti);
        }


        [Fact]
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
            Assert.Equal(pt, yeti);
        }

        #endregion

    }
}
