using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using Serializer.Tests;

namespace Serializer.Benchmarks
{
    public class JsonBenchmarks
    {
        static JsonBenchmarks()
        {
            Serializer = new DasSerializer();

            SimpleClassObjectProperty nullPayload = SimpleClassObjectProperty.GetNullPayload();
            NullPayloadJson = Serializer.ToJson(nullPayload);
        }


        [Benchmark]
        public SimpleClassObjectProperty PrimitivePropertiesJsonBaseline()
        {
            return Serializer.FromJson<SimpleClassObjectProperty>(NullPayloadJson);
        }

        [Benchmark]
        public SimpleClassObjectProperty PrimitivePropertiesJsonExpress()
        {
            return Serializer.FromJson<SimpleClassObjectProperty>(NullPayloadJson);
        }

        private static readonly DasSerializer Serializer;

        private static readonly String NullPayloadJson;
    }
}
