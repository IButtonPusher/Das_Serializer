using System;
using Das;

namespace UnitTestProject1
{
    public abstract class TestBase
    {
        private DasSerializer _serializer;
        protected DasSerializer Serializer => _serializer ?? (_serializer = new DasSerializer());
    }
}
