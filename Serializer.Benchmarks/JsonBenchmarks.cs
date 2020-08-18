using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using Serializer.Tests;

namespace Serializer.Benchmarks
{
    public class JsonBenchmarks
    {
        private static DasSerializer Serializer;

        private static SimpleClass NullPayload;
        private static String NullPayloadJson;

        static JsonBenchmarks()
        {
            Serializer = new DasSerializer();

            NullPayload = SimpleClass.GetNullPayload();
            NullPayloadJson = Serializer.ToJson(NullPayload);
        }


        [Benchmark]
        public SimpleClass PrimitivePropertiesJsonBaseline()
        {
            return Serializer.FromJson<SimpleClass>(NullPayloadJson);
        }

        [Benchmark]
        public SimpleClass PrimitivePropertiesJsonExpress()
        {
            return Serializer.FromJsonEx<SimpleClass>(NullPayloadJson);
        }
        
    }
}
