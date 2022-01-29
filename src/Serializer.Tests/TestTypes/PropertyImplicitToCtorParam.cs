using System;
using System.Threading.Tasks;

namespace Serializer.Tests.TestTypes
{
    public class PropertyImplicitToCtorParam1
    {
        public PropertyImplicitToCtorParam1(ImplicitlyConvertible1 myProperty)
        {
            MyProperty = myProperty;
        }

        //public static implicit operator PropertyImplicitToCtorParam2(PropertyImplicitToCtorParam1 me)
        //{
        //    return new PropertyImplicitToCtorParam2(me.MyProperty);
        //}

        public ImplicitlyConvertible1 MyProperty { get; }
    }

    public class PropertyImplicitToCtorParam2
    {
        public PropertyImplicitToCtorParam2(ImplicitlyConvertible2 myProperty)
        {
            MyProperty = myProperty;
        }

        public ImplicitlyConvertible2 MyProperty { get; }
    }

    public class ImplicitlyConvertible1
    {
        public static implicit operator ImplicitlyConvertible2(ImplicitlyConvertible1 me)
        {
            return new ImplicitlyConvertible2();
        }
    }

    public class ImplicitlyConvertible2
    {
        public static implicit operator ImplicitlyConvertible1(ImplicitlyConvertible2 me)
        {
            return new ImplicitlyConvertible1();
        }
    }
}
