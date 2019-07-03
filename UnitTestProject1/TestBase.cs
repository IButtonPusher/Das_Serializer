using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Das;

namespace UnitTestProject1
{
    public abstract class TestBase
    {
        private DasSerializer _serializer;
        protected DasSerializer Serializer => _serializer ?? (_serializer = new DasSerializer());
    }
}
