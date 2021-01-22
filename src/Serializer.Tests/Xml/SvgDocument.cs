using System;
using System.Threading.Tasks;

namespace Serializer.Tests.Xml
{
    public class SvgDocument
    {
        public Int32 Width { get;set; }

        public Int32 Height { get;set; }

        public String? ViewBox { get;set; }

        public SvgPath? Path { get; set; }
    }
}
