using System;
using System.Collections.Generic;
using Das.Scanners;

namespace Das.Serializer
{
    public interface IBinaryScanner : IScannerBase<IEnumerable<Byte[]>>,
        IScannerBase<IByteArray>
    {
    }
}
