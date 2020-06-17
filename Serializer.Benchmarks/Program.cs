﻿using System;
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
        public static void Main(String[] args)
        {
#if DEBUG
            RunManyTimes();
#endif
#if !DEBUG
           if (args == null || args.Length == 0)
               BenchmarkRunner.Run<ProtoBufTests>();
           else
               RunManyTimes();
#endif

        }

        [ThreadStatic]
        private static Byte[]? _rdrr;

        private unsafe static void RunManyTimes()
        {
            _rdrr = _rdrr ?? new Byte[256];

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
                        buff.DasSimpleMessage();
                        buff.DasDoubleMessage();
                        buff.DasStringMessage();
                        buff.DasDictionary();
                        buff.DasNegativeIntegerMessage();
                        buff.DasMultiProperties();
                        buff.DasComposedMessage();

                        buff.DasByteArray();

                        //buff.TypeProvider.DumpProxies();
                    }


                    Debug.WriteLine(" das elapsed: " + sw.ElapsedMilliseconds);
                }
            }

            for (var i = 0; i < 1000; i++)
            {
               
            }


            Debug.WriteLine(" das TOTAL: " + swo.ElapsedMilliseconds);
        }

     

        public class Benchies : TestBase
        {
            private String _xmlString;
            private Char[] _xmlEnumerable;

            public Benchies()
            {
                var mc1 = ObjectDictionary.Get();

                var xml = Serializer.ToXml(mc1);
                _xmlString = xml;
                _xmlEnumerable = xml.ToCharArray();
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
