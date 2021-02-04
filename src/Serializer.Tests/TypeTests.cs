﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Das.Serializer;
using Xunit;

#pragma warning disable 8602

// ReSharper disable All

namespace Serializer.Tests
{
    public class TypeTests : TestBase
    {
        [Fact]
        public void AssemblyType()
        {
            var type = typeof(DasSerializer);
            
            var str = Serializer.TypeInferrer.ToClearName(type);
            var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
            Assert.Equal(type, type2);

            type = typeof(DasSettings);
            str = Serializer.TypeInferrer.ToClearName(type);
            type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
            Assert.Equal(type, type2);
        }

        [Fact]
        public void CastDynamicViaImplicitOperator()
        {
            var saver = new StringSaver();
            var sb = Serializer.ObjectManipulator.CastDynamic<StringBuilder>(saver);
        }

        [Fact]
        public void ChangeStringCaseStyles()
        {
            var pascalCase = "SiteId";
            var asSnake = Serializer.TypeInferrer.ToSnakeCase(pascalCase);
            Assert.True(asSnake == "site_id");

            var asCamel = Serializer.TypeInferrer.ToCamelCase(pascalCase);
            Assert.True(asCamel == "siteId");

            var snakeCase = "hand_no";
            asCamel = Serializer.TypeInferrer.ToCamelCase(snakeCase);
            Assert.True(asCamel == "handNo");

            var asPascal = Serializer.TypeInferrer.ToPascalCase(snakeCase);
            Assert.True(asPascal == "HandNo");

            var camelCase = "tableName";
            asSnake = Serializer.TypeInferrer.ToSnakeCase(camelCase);
            Assert.True(asSnake == "table_name");

            asPascal = Serializer.TypeInferrer.ToPascalCase(camelCase);
            Assert.True(asPascal == "TableName");
        }


        [Fact]
        public void GenericType()
        {
            var type = typeof(List<String>);
            var str = Serializer.TypeInferrer.ToClearName(type);
            Serializer.TypeInferrer.ClearCachedNames();
            var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str, true);
            Assert.Equal(type, type2);


            type = typeof(Dictionary<String, Random>);
            str = Serializer.TypeInferrer.ToClearName(type);
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
            str = Serializer.TypeInferrer.ToClearName(type);
            Serializer.TypeInferrer.ClearCachedNames();
            type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
            Assert.Equal(type, type2);

            type = typeof(GenericClass<SimpleClassObjectProperty>);
            str = Serializer.TypeInferrer.ToClearName(type);
            Serializer.TypeInferrer.ClearCachedNames();
            type2 = Serializer.TypeInferrer.GetTypeFromClearName(str, true);
            Assert.Equal(type, type2);

            type = typeof(Dictionary<string, List<decimal>>);
            str = Serializer.TypeInferrer.ToClearName(type);
            Serializer.TypeInferrer.ClearCachedNames();
            type2 = Serializer.TypeInferrer.GetTypeFromClearName(str, true);
            Assert.Equal(type, type2);
        }


        [Fact]
        public void NamespaceType()
        {
            var type = typeof(Encoding);
            var str = Serializer.TypeInferrer.ToClearName(type);
            var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
            Assert.Equal(type, type2);

            type = typeof(IEnumerable);
            str = Serializer.TypeInferrer.ToClearName(type);
            type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
            Assert.Equal(type, type2);
        }

        [Fact]
        public void PrimitiveType()
        {
            var type = typeof(Int32);
            var str = Serializer.TypeInferrer.ToClearName(type);
            var type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
            Assert.Equal(type, type2);

            type = typeof(String);
            str = Serializer.TypeInferrer.ToClearName(type);
            type2 = Serializer.TypeInferrer.GetTypeFromClearName(str);
            Assert.Equal(type, type2);
        }

        #if !TEST_NO_CODEGENERATION

        [Fact]
        public void PropertyGetters()
        {
            for (var c = 0; c < 5; c++)
            {
                var getter = TypeManipulator.CreateDynamicPropertyGetter(typeof(ISimpleClass),
                    nameof(ISimpleClass.Name));

                var inst = new SimpleClass2 {Name = "bobbith"};

                var testRes = getter(inst);
                Assert.Equal("bobbith", testRes);

                var ezProp = typeof(TestCompositeClass).GetProperty(
                    nameof(TestCompositeClass.SimpleLeft));
                var func = TypeManipulator.CreateDynamicPropertyGetter(typeof(TestCompositeClass),
                    ezProp!);

                var obj = TestCompositeClass.Init();

                var res = func(obj);
                Assert.Equal(res, obj.SimpleLeft);


                var func2 = TypeManipulator.CreateDynamicPropertyGetter(typeof(TestCompositeClass),
                    nameof(TestCompositeClass.SimpleLeft) + "." +
                    nameof(SimpleClassObjectProperty.Name));


                var res2 = func2(obj);
                Assert.Equal(res2, obj.SimpleLeft.Name);

                var accessor = Serializer.TypeManipulator.GetPropertyAccessor(typeof(TestCompositeClass),
                    nameof(TestCompositeClass.SimpleLeft));

                var res3 = accessor.GetPropertyValue(obj);
                Assert.Equal(res3, obj.SimpleLeft);
            }
        }

        

        [Fact]
        public void PropertySetters()
        {
            //var nameProp = typeof(SimpleClass).GetProperty(nameof(SimpleClass.Name));
            //var setter = TypeManipulator.CreateDynamicSetter(nameProp);

            //var inst = SimpleClass.GetExample();
            //Object instance = inst;

            //setter(ref instance, "suzy slammer");
            //Assert.Equal("suzy slammer", inst.Name);

            var inst2 = TestCompositeClass.Init();
            Object instance2 = inst2;

            var func2 = TypeManipulator.CreateDynamicSetter(typeof(TestCompositeClass),
                nameof(TestCompositeClass.SimpleLeft) + "." +
                nameof(SimpleClassObjectProperty.Name));

            func2(ref instance2!, "wiley wamboozle");
            
            Assert.Equal("wiley wamboozle", inst2.SimpleLeft.Name);
        }

        #endif
    }
}
