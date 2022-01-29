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
            SimpleClass = SimpleClass.GetExample<SimpleClass>();
            //NullPayloadJson = Serializer.ToJson(NullPayload);
        }

      

      

       


        //[Benchmark]
        //public SimpleClassObjectProperty PrimitivePropertiesJsonBaseline()
        //{
        //    return Serializer.FromJson<SimpleClassObjectProperty>(NullPayloadJson);
        //}


       
        [Benchmark]
        public String PrintDasDictionary()
        {
            var mc1 = DictionaryPropertyMessage.DefaultValue;
            return Serializer.ToJsonEx(mc1);
        }

        [Benchmark]
        public String JsonNetPrintDictionary()
        {
            var mc1 = DictionaryPropertyMessage.DefaultValue;
            return JsonConvert.SerializeObject(mc1);
        }

        [Benchmark]
        public DictionaryPropertyMessage DasDictionary()
        {
            var mc1 = DictionaryPropertyMessage.DefaultValue;
            var json = Serializer.ToJsonEx(mc1);

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
            var json = Serializer.ToJsonEx(msg);

            return Serializer.FromJson<MultiPropMessage>(json);
        }

        [Benchmark]
        public MultiPropMessage JsonNetMultiProperties()
        {
            var msg = MultiPropMessage.GetTestOne();
            var json = JsonConvert.SerializeObject(msg);

            return JsonConvert.DeserializeObject<MultiPropMessage>(json);
        }


        [Benchmark]
        public SimpleClass DasPrimitiveProperties()
        {
            var json = Serializer.ToJsonEx(SimpleClass);
            return Serializer.FromJson<SimpleClass>(json);
        }

        //[Benchmark]
        //public SimpleClass DasPrimitiveProperties2()
        //{
        //    var json = Serializer.ToJsonEx(SimpleClass);
        //    return Serializer.FromJson<SimpleClass>(json);
        //}

        [Benchmark]
        public SimpleClass JsonNetPrimitiveProperties()
        {
            var json = JsonConvert.SerializeObject(SimpleClass);
            return JsonConvert.DeserializeObject<SimpleClass>(json);
        }

        [Benchmark]
        public String DasPrintPrimitiveProperties()
        {
            return Serializer.ToJsonEx(SimpleClass);
        }

        //[Benchmark]
        //public String DasPrintPrimitiveProperties2()
        //{
        //    return Serializer.ToJsonEx(SimpleClass);
        //}

        [Benchmark]
        public String JsonNetPrintPrimitiveProperties()
        {
            return JsonConvert.SerializeObject(SimpleClass);
        }

        private static readonly DasSerializer Serializer;
        private static readonly SimpleClassObjectProperty NullPayload;
        private static readonly SimpleClass SimpleClass;

        //private static readonly String NullPayloadJson;
    }
}
