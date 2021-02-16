using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using Newtonsoft.Json;
using Serializer.Tests;

namespace Serializer.Benchmarks
{
    public class JsonBenchmarks
    {
        static JsonBenchmarks()
        {
            Serializer = new DasSerializer();

            NullPayload = SimpleClassObjectProperty.GetNullPayload();
            
        }


        //[Benchmark]
        //public SimpleClassObjectProperty PrimitivePropertiesJsonBaseline()
        //{
        //    return Serializer.FromJson<SimpleClassObjectProperty>(NullPayloadJson);
        //}

        [Benchmark]
        public SimpleClassObjectProperty DasPrimitivePropertiesJson()
        {
            var NullPayloadJson = Serializer.ToJson(NullPayload);
            return Serializer.FromJson<SimpleClassObjectProperty>(NullPayloadJson);
        }

        [Benchmark]
        public SimpleClassObjectProperty JsonNetPrimitivePropertiesJson()
        {
            var NullPayloadJson = JsonConvert.SerializeObject(NullPayload);
            return JsonConvert.DeserializeObject<SimpleClassObjectProperty>(NullPayloadJson);
            //return Serializer.FromJson<SimpleClassObjectProperty>(NullPayloadJson);
        }

        private static readonly DasSerializer Serializer;
        private static readonly SimpleClassObjectProperty NullPayload;

        //private static readonly String NullPayloadJson;
    }
}
