using System;
using System.Threading.Tasks;
using Das.Types;
using Xunit;
// ReSharper disable All
#pragma warning disable 8600

namespace Serializer.Tests
{
    public class ExpressionTests
    {
        [Fact]
        public void PropertyGetters()
        {
            var ezProp = typeof(TestCompositeClass).GetProperty(
                nameof(TestCompositeClass.SimpleLeft));
            var func = TypeManipulator.CreateExpressionPropertyGetter(typeof(TestCompositeClass),
                ezProp!);
            
            var obj = TestCompositeClass.Init();

            var res = func(obj);
            Assert.Equal(res, obj.SimpleLeft);
            
            
            var func2 = TypeManipulator.CreateExpressionPropertyGetter(typeof(TestCompositeClass),
                nameof(TestCompositeClass.SimpleLeft) + "." +
                nameof(SimpleClassObjectProperty.Name));

            
            var res2 = func2(obj);
            Assert.Equal(res2, obj.SimpleLeft.Name);
        }

        [Fact]
        public void PropertySetters()
        {
            var nameProp = typeof(SimpleClass).GetProperty(nameof(SimpleClass.Name));
            var setter = TypeManipulator.CreateExpressionPropertySetter(nameProp);

            var inst = SimpleClass.GetExample();
            Object instance = inst;

            setter(ref instance, "suzy slammer");
            Assert.Equal("suzy slammer", inst.Name);

            var inst2 = TestCompositeClass.Init();
            Object instance2 = inst2;
            
            var func2 = TypeManipulator.CreateExpressionPropertySetter(typeof(TestCompositeClass),
                nameof(TestCompositeClass.SimpleLeft) + "." +
                nameof(SimpleClassObjectProperty.Name));

            func2(ref instance2, "wiley wamboozle");
            
            Assert.Equal("wiley wamboozle", inst2.SimpleLeft.Name);
        }
    }
}
