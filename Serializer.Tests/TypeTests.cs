using Das.Extensions;
using Das.Serializer;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

// ReSharper disable All

namespace Serializer.Tests
{
	//[TestClass]
	public class TypeTests : TestBase
	{
		//[TestCategory("primitive"), TestCategory("types"), TestMethod]
		public void PrimitiveType()
		{
			var type = typeof(Int32);
			var str = type.GetClearName(false);
			var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
			Assert.Equal(type, type2);

			type = typeof(String);
			str = type.GetClearName(false);
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
			Assert.Equal(type, type2);
		}

		//[TestCategory("namespace"), TestCategory("types"), TestMethod]
		public void NamespaceType()
		{
			var type = typeof(Encoding);
			var str = type.GetClearName(false);
			var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
			Assert.Equal(type, type2);

			type = typeof(IEnumerable);
			str = type.GetClearName(false);
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
			Assert.Equal(type, type2);
		}

		//[TestCategory("assembly"), TestCategory("types"), TestMethod]
		public void AssemblyType()
		{
			var type = typeof(DasSerializer);
			var str = type.GetClearName(false);
			var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
			Assert.Equal(type, type2);

			type = typeof(DasSettings);
			str = type.GetClearName(false);
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
			Assert.Equal(type, type2);
		}

		//[TestCategory("generic"), TestCategory("types"), TestMethod]
		public void GenericType()
		{
			//var extType = typeof(ExtensionMethods);

			var type = typeof(List<String>);
			var str = type.GetClearName(false);
			Serializer.TypeInferrer.ClearCachedNames();
			var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str, true);
			Assert.Equal(type, type2);


			type = typeof(Dictionary<String, Random>);
			str = type.GetClearName(false);
			Serializer.TypeInferrer.ClearCachedNames();
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(str, true);
			Assert.Equal(type, type2);

			var fullName = type.FullName;
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(fullName, true);
			Assert.Equal(type, type2);

			type = typeof(Dictionary<String, SimpleClassObjectProperty>);
            if (type?.FullName == null)
                throw new Exception();
			var wrongName = type.FullName.Replace("Serializer.Tests", "Serializer.Tests2");
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(wrongName);
			Assert.NotEqual(type, type2);

			type = typeof(Object[]);
			str = type.GetClearName(false);
			Serializer.TypeInferrer.ClearCachedNames();
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
			Assert.Equal(type, type2);

			type = typeof(GenericClass<SimpleClassObjectProperty>);
			str = type.GetClearName(false);
			Serializer.TypeInferrer.ClearCachedNames();
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(str, true);
			Assert.Equal(type, type2);

			type = typeof(Dictionary<string, List<decimal>>);
			str = type.GetClearName(false);
			Serializer.TypeInferrer.ClearCachedNames();
			type2 = Serializer.TypeInferrer.GetTypeFromClearName(str, true);
			Assert.Equal(type, type2);
		}
	}
}
