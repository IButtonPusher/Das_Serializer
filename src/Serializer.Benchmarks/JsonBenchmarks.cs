using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using Newtonsoft.Json;
using Serializer.Tests;
// ReSharper disable All

namespace Serializer.Benchmarks
{
    public class JsonBenchmarks
    {
        static JsonBenchmarks()
        {
            var settings = DasSettings.CloneDefault();
            settings.CircularReferenceBehavior = CircularReference.NoValidation;
            Serializer = new DasSerializer(settings);

            NullPayload = SimpleClassObjectProperty.GetNullPayload();
            NullPayloadJson = Serializer.ToJson(NullPayload);
        }


        //[Benchmark]
        //public SimpleClassObjectProperty PrimitivePropertiesJsonBaseline()
        //{
        //    return Serializer.FromJson<SimpleClassObjectProperty>(NullPayloadJson);
        //}

        public String DasPrintPrimitiveProperties()
        {
            return Serializer.ToJson(NullPayload);
        }

        [Benchmark]
        public SimpleClassObjectProperty DasPrimitiveProperties()
        {
            var nullPayloadJson = Serializer.ToJson(NullPayload);
            return Serializer.FromJson<SimpleClassObjectProperty>(nullPayloadJson);
        }

        [Benchmark]
        public SimpleClassObjectProperty JsonNetPrimitivePropertiesJson()
        {
            var nullPayloadJson = JsonConvert.SerializeObject(NullPayload);
            return JsonConvert.DeserializeObject<SimpleClassObjectProperty>(nullPayloadJson);
            
        }

        private static readonly DasSerializer Serializer;
        private static readonly SimpleClassObjectProperty NullPayload;

        private static readonly String NullPayloadJson;
    }
}
