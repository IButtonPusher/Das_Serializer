using System;
using System.Collections.Generic;
using System.Text;

namespace Das.Serializer
{
    public partial class DasCoreSerializer
    {
        public DasCoreSerializer() : this(new CoreStateProvider(),
            WriteAsync, ReadToEndAsync, ReadAsync){}
    }
}
