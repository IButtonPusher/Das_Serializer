using System;
using System.Linq;
using UnitTestProject1;

namespace Serializer.Benchmarks
{
    class Program
    {
        static void Main()
        {
            var methods = typeof(XmlTests).GetMethods().
                Where(m => m.CustomAttributes.Count() > 1).ToArray();
            var test = new XmlTests();
            var empty = new Object[0];

            foreach (var method in methods)
                method.Invoke(test, empty);

        }
    }
}
