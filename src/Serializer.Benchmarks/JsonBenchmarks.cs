using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Das.Serializer;
using Newtonsoft.Json;
using Serializer.Tests;
using Serializer.Tests.ProtocolBuffers;

// ReSharper disable All

namespace Serializer.Benchmarks
{
    public class JsonBenchmarks
    {
        static JsonBenchmarks()
        {
            //var settings = DasSettings.CloneDefault();
            //settings.CircularReferenceBehavior = CircularReference.NoValidation;
            Serializer = new DasSerializer();

            NullPayload = SimpleClassObjectProperty.GetNullPayload();
            //NullPayloadJson = Serializer.ToJson(NullPayload);
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
            var json = Serializer.ToJson(NullPayload);
            return Serializer.FromJson<SimpleClassObjectProperty>(json);
        }

        [Benchmark]
        public SimpleClassObjectProperty JsonNetPrimitivePropertiesJson()
        {
            var nullPayloadJson = JsonConvert.SerializeObject(NullPayload);
            return JsonConvert.DeserializeObject<SimpleClassObjectProperty>(nullPayloadJson);
        }

        [Benchmark]
        public DictionaryPropertyMessage DasDictionary()
        {
            var mc1 = DictionaryPropertyMessage.DefaultValue;
            var json = Serializer.ToJson(mc1);

            return Serializer.FromJson<DictionaryPropertyMessage>(json);
        }

        [Benchmark]
        public DictionaryPropertyMessage JsonNetDictionary()
        {
            var mc1 = DictionaryPropertyMessage.DefaultValue;
            var json = JsonConvert.SerializeObject(mc1);

            return JsonConvert.DeserializeObject<DictionaryPropertyMessage>(json);
        }

        [Benchmark]
        public MultiPropMessage DasMultiProperties()
        {
            var msg = MultiPropMessage.GetTestOne();
            var json = Serializer.ToJson(msg);

            return Serializer.FromJson<MultiPropMessage>(json);
        }

        [Benchmark]
        public MultiPropMessage JsonNetMultiProperties()
        {
            var msg = MultiPropMessage.GetTestOne();
            var json = JsonConvert.SerializeObject(msg);

            return JsonConvert.DeserializeObject<MultiPropMessage>(json);
        }

        private static readonly DasSerializer Serializer;
        private static readonly SimpleClassObjectProperty NullPayload;

        //private static readonly String NullPayloadJson;
    }
}
