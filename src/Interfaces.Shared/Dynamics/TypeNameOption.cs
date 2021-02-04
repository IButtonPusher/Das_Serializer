using System;
using System.Collections.Generic;
using System.Text;

namespace Das.Serializer
{
    [Flags]
    public enum TypeNameOption
    {
        Invalid = 0,
        AssemblyName = 1,
        Namespace = 2,
        OmitGenericArguments = 4
    }
}
