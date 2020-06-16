using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public class DasAttribute
    {
        public DasAttribute(Type type)
        {
            Type = type;
            PropertyValues = new Dictionary<String, Object>();
        }

        public Object[] ConstructionValues { get; set; }

        public Dictionary<String, Object> PropertyValues { get;  }

        public Type Type { get; }
    }
}