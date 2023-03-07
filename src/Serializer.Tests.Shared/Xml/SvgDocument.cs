using System;
using System.Threading.Tasks;

// ReSharper disable All

namespace Serializer.Tests.Xml
{
    public class SvgDocument
    {
        public Int32 Height { get; set; }

        public SvgPath? Path { get; set; }

        public String? ViewBox { get; set; }

        public Int32 Width { get; set; }
    }
}
