using Das;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Das.Extensions;
// ReSharper disable All
#pragma warning disable 8625
#pragma warning disable 8618

namespace Serializer.Tests
{
	public interface ISimpleClass
	{
		Int32 ID { get; set; }
		String Name { get; set; }
		Decimal GPA { get; set; }
		Object Payload { get; set; }
	}


    public class SimpleClass
    {
        public Int32 ID { get; set; }
        public String Name { get; set; }
        public Decimal GPA { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime HireDate { get; set; }

        public TimeSpan ShiftPreference { get; set; }

        public Animals Animal { get; set; }

        public static T GetExample<T>() where T : SimpleClass, new()
        {
            return new T
            {
                ID = 345,
                Name = "Bobby Tables",
                GPA = 3.14M,
                DateOfBirth = new DateTime(1980, 9, 4),
                HireDate = new DateTime(2000, 12, 21),
                ShiftPreference = TimeSpan.FromHours(9),
                Animal = Animals.Sheep
            };
        }

        public static SimpleClass GetExample() => GetExample<SimpleClass>();
    }

    public class SimpleClassWithPrimitiveCollection : SimpleClass
    {
		public List<Int32> LuckyNumbers { get; set; }

        public SimpleClassWithPrimitiveCollection()
        {
            LuckyNumbers = new List<int>();
        }

        public new static SimpleClassWithPrimitiveCollection GetExample()
        {
            var res = GetExample<SimpleClassWithPrimitiveCollection>();
			res.LuckyNumbers.AddRange(new []{6,14,77});

            return res;
        }
    }

    public class SimpleClassWithObjectCollection : SimpleClass
    {
        public List<SimpleClass> AllMySimpletions { get; set; }

        public SimpleClassWithObjectCollection()
        {
            AllMySimpletions = new List<SimpleClass>();
        }

        public new static SimpleClassWithObjectCollection GetExample()
        {
            var res = GetExample<SimpleClassWithObjectCollection>();
			res.DateOfBirth = new DateTime(1977, 6, 14);

            var s1 = SimpleClass.GetExample();
			res.AllMySimpletions.Add(s1);

            var s2 = SimpleClass.GetExample();
            s2.Name = "May Lou";
            s2.Animal = Animals.Cow;
            s2.GPA = 11.0M;

            res.AllMySimpletions.Add(s2);

            return res;
        }
    }


	public class SimpleClassObjectProperty : SimpleClass, ISimpleClass
	{
        public Object Payload { get; set; }

        public String SuperSecret { get; }


        public SimpleClassObjectProperty(String withSecret) : this()
		{
			SuperSecret = withSecret;
		}

		public SimpleClassObjectProperty()
		{
			HireDate = new DateTime(1996, 6, 14);
			ShiftPreference = new TimeSpan(8, 0, 0);
			
		}

		public static SimpleClassObjectProperty GetNullPayload()
		{
			var sc = new SimpleClassObjectProperty
			{
				GPA = 4.01M,
				ID = 43,
				Name = "bo<bby \"} tables]",
				Animal = Animals.Cow
			};
			return sc;
		}

		public static SimpleClassObjectProperty GetPrimitivePayload()
		{
			var sc = new SimpleClassObjectProperty
			{
				GPA = 2.67M,
				ID = 4300,
				Name = null,
				Payload = 3.4M,
				DateOfBirth = new DateTime(1969, 11, 24)
			};

			return sc;
		}

		public override bool Equals(object obj)
		{
			var sc = obj as SimpleClassObjectProperty;
			if (sc == null)
				return false;
			return ID == sc.ID && Name == sc.Name && GPA == sc.GPA && Object.Equals(Payload,
				sc.Payload);
		}

		public override int GetHashCode()
		{
			return ID;
		}
	}

	public class SimpleClass2 : ISimpleClass
	{
		public Int32 ID { get; set; }
		public String Name { get; set; }
		public Decimal GPA { get; set; }
		public Object Payload { get; set; }
	}

	public class SimpleContainer
	{
		[SerializeAsType(typeof(ISimpleClass))]
		public ISimpleClass SimpleExample { get; set; }
	}

	public enum Animals : Byte
	{
		Pig,
		Dog,
		Sheep,
		Cow,
		Frog,
		Cat
	}

	public class GenericClass<T>
	{
		public HashSet<String> GenericProperty { get; set; }

		public GenericClass()
		{
			GenericProperty = new HashSet<string>();
		}
	}

	public class Teacher
	{
		public static Teacher Get()
		{
			var teacher = new Teacher();
            var student = new Student { MiddleName = "Hank" };
            teacher.Pupils = new List<Student> { student };
            teacher.Pupils.ForEach(p => p.MathTeacher = teacher);
			return teacher;
		}

		public IEnumerable<Student> Pupils { get; set; }
		public String FirstName { get; set; }

		public Teacher()
		{
			FirstName = "Ben";
            Pupils =new List<Student>();

        }
	}

	public class Student
	{
		public Teacher MathTeacher { get; set; }
		public String MiddleName { get; set; }

		public Student()
		{

		}
	}

	#region collections

	public class PrimitiveArray
	{
		public String[] StringArray { get; set; }

		public static PrimitiveArray Get()
		{
			return new PrimitiveArray
			{
				StringArray = new String[] { "s1", "s2", "fsdhfjswehset" }
			};
		}

	}

	public class PrimitiveList
	{
		public List<Decimal> DecimalList { get; set; }

		public static PrimitiveList Get()
		{
			return new PrimitiveList
			{
				DecimalList = new List<Decimal> { 0.5M, 1.74M, 3.14M }
			};
		}

	}

	public class ObjectArray
	{
		public SimpleClassObjectProperty[] ItemArray { get; set; }

		public static ObjectArray Get()
		{
			return new ObjectArray
			{
				ItemArray = new SimpleClassObjectProperty[]
			{
				new SimpleClassObjectProperty { GPA = 5.3M, ID = 4, Name = "bob", Payload = "fff" },
				new SimpleClassObjectProperty { GPA = 2.9M, ID = 74, Name = "suzy", Payload = "5555" }
			}
			};

		}

	}



	public class ObjectList
	{
		public List<SimpleClassObjectProperty> ItemList { get; set; }

		public static ObjectList Get()
		{
			return new ObjectList
			{
				ItemList = new List<SimpleClassObjectProperty>
					{
						new SimpleClassObjectProperty { GPA = 5.3M, ID = 4, Name = "bob", Payload = "fff" },
						new SimpleClassObjectProperty { GPA = 2.9M, ID = 74, Name = "suzy", Payload = "5555" }
					}
			};

		}
	}

	public class StringArray
	{
		public String[] SomeStrings { get; set; }

	}

	public class ObjectDictionary
	{
		public Dictionary<Object, Double> Dic { get; set; }

		public ObjectDictionary()
		{
			Dic = new Dictionary<object, double>();
		}

		public static ObjectDictionary Get()
		{
			var mc1 = new ObjectDictionary
			{
				Dic = new Dictionary<object, double>()
			};

			mc1.Dic.Add("asdf", 3);
			mc1.Dic.Add(44, 3.14);

			return mc1;

		}

	}

	public class ObjectConcurrentDictionary
	{
		public ConcurrentDictionary<String, Int32> Dic { get; set; }

		public ObjectConcurrentDictionary()
		{
			Dic = new ConcurrentDictionary<String, Int32>();
		}

		public static ObjectConcurrentDictionary Get()
		{
			var mc1 = new ObjectConcurrentDictionary();

			mc1.Dic.TryAdd("asdf", 3);
			mc1.Dic.TryAdd("44fo", 187);

			return mc1;

		}

	}

	#endregion

	public class TestCompositeClass : ITestCompositeClass
	{
		public SimpleClassObjectProperty SimpleLeft { get; set; }
		public SimpleClassObjectProperty SimpleRight { get; set; }

		private TestCompositeClass()
		{

		}

		public static TestCompositeClass Init()
		{
			var tcc = new TestCompositeClass();
			tcc.SimpleLeft = new SimpleClassObjectProperty
			{
				ID = 6,
				Name = "Some really long name that will use many bytes",
				GPA = 1.23M,
				Animal = Animals.Dog
			};
			tcc.SimpleRight = new SimpleClassObjectProperty
			{
				ID = -5,
				Name = null,
				GPA = 0,
				Payload = 2.3f,
				DateOfBirth = new DateTime(1979, 12, 31)
			};

			return tcc;
		}
	}

	public class UTypes
	{
		public SByte SByte { get; set; }
		public UInt16 U16 { get; set; }
		public UInt32 U32 { get; set; }
		public UInt64 U64 { get; set; }
	}

	[SerializeAsType(typeof(AbstractComposite))]
	public class TestCompositeClass2 : AbstractComposite, ITestCompositeClass
	{
		public SimpleClassObjectProperty SimpleLeft { get; set; }


		public TestCompositeClass2()
		{
			SimpleLeft = new SimpleClassObjectProperty();
			SimpleRight = new SimpleClassObjectProperty();
		}
	}

	public interface ITestCompositeClass
	{
		SimpleClassObjectProperty SimpleLeft { get; set; }
	}

	public abstract class AbstractComposite
	{
		public SimpleClassObjectProperty SimpleRight { get; set; }
	}

	public class DeferredProperty
	{
		public Object SimpleProperty
		{
			get
			{
				return GetMethod();
			}
			set
			{
				//something...
			}
		}

		private IEnumerable<ISimpleClass> GetMethod()
		{
			for (var i = 0; i < 10; i++)
			{
				yield return SimpleClassObjectProperty.GetNullPayload();
				yield return SimpleClassObjectProperty.GetPrimitivePayload();
			}
		}
	}

    public class ResponseIdTest
    {
		public Int32 id { get; set; }
		public BaseResponseData result { get; set; }
    }

    public class BaseResponseData
    {
		public ResponseError error { get; set; }
    }

    public class ResponseError
    {
        public Int32 code { get; set; }

        public String message { get; set; }
    }

    public class ArticleDto 
    {
        public ArticleDto(Int32 id, 
                          String headline, 
                          String url, 
                          Int32 siteId, 
                          Int32? categoryId, 
                          Int64 signature,
                          String signatureDescription, 
                          Int32 score)
        {
            Id = id;
            Headline = headline;
            Url = url;
            SiteId = siteId;
            CategoryId = categoryId;
            Signature = signature;
            SignatureDescription = signatureDescription;
            
            Score = score;
        }

        public Int32 Id { get; }

        public String Headline { get; }

        public String Url { get; }

        public Int32 SiteId { get; }

        public Int32? CategoryId { get; }

        public Int64 Signature { get; }

        public String SignatureDescription { get; }

        //public Boolean IsRead { get; }

        public Int32 Score { get; }
    }
}
