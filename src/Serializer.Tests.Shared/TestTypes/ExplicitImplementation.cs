using System;

namespace Serializer.Tests.TestTypes
{
    public interface IWillBeExplicit
    {
        Int32 IntProp { get; }

        String StringProp { get; }
    }

    public class ExplicitImplementation : IWillBeExplicit
    {
        public ExplicitImplementation()
        {
            IntProp = 45;
            StringProp = "String Value";
        }

        Int32 IWillBeExplicit.IntProp => IntProp;

        public Int32 IntProp { get; }

        String IWillBeExplicit.StringProp => StringProp;

        public String StringProp { get; }
    }
}
