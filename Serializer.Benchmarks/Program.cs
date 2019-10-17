using System;

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using UnitTestProject1;
// ReSharper disable All

namespace Serializer.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            var summary = BenchmarkRunner.Run<Benchies>();

            //var methods = typeof(XmlTests).GetMethods().
            //                Where(m => m.CustomAttributes.Count() > 1).ToArray();
            //            var test = new XmlTests();
            //            var empty = new Object[0];
            //
            //            foreach (var method in methods)
            //                method.Invoke(test, empty);
        }

        public class Benchies : TestBase
        {
            private String _xmlString;
            private IEnumerable<Char> _xmlEnumerable;

            public Benchies()
            {
                var mc1 = ObjectDictionary.Get();

                var xml = Serializer.ToXml(mc1);
                _xmlString = xml;
                _xmlEnumerable = xml;
            }

            [Benchmark]
            public ObjectDictionary DeserializeCharArray()
            {
                return Serializer.FromXml<ObjectDictionary>(_xmlString);
            }

            [Benchmark]
            public ObjectDictionary DeserializeEnumerable()
            {
                return Serializer.FromXml<ObjectDictionary>(_xmlEnumerable);
            }
        }
    }
}
