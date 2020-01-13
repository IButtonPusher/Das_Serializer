using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
// ReSharper disable once RedundantUsingDirective
using BenchmarkDotNet.Running;
using Serializer.Tests;
using Serializer.Tests.ProtocolBuffers;

// ReSharper disable All

namespace Serializer.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
#if DEBUG
            RunManyTimes();
#endif
#if !DEBUG
           BenchmarkRunner.Run<ProtoBufTests>();
#endif

        }

        private static void RunManyTimes()
        {
            var buff = new ProtoBufTests();
            var swo = new Stopwatch();
            swo.Start();


            for (var i = 0; i < 1000; i++)
            {
                {
                    var sw = new Stopwatch();
                    sw.Start();


                    for (var c = 0; c < 10000; c++)
                    {
                        
                        buff.DasByteArray();
                        buff.DasComposedMessage();
                        buff.DasDictionary();
                         buff.DasMultiProperties();
                         buff.DasNegativeIntegerMessage();
                         buff.DasDoubleMessage();
                         buff.DasSimpleMessage();
                         buff.DasStringMessage();
                    }


                    Debug.WriteLine(" das elapsed: " + sw.ElapsedMilliseconds);
                }

//                {
//                    var sw = new Stopwatch();
//                    sw.Start();
//
//
//                    for (var c = 0; c < 10000; c++)
//                    {
//                        //buff.ProtoNetByteArray();
//                        //buff.ProtoNetSimpleMessage();
//                        msg = buff.ProtoNetByteArray();
//                    }
//
//
//                    Debug.WriteLine(" proto elapsed: " + sw.ElapsedMilliseconds);
//                }
            }

            for (var i = 0; i < 1000; i++)
            {
               
            }


            Debug.WriteLine(" das TOTAL: " + swo.ElapsedMilliseconds);
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
