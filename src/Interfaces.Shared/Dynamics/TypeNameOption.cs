using System;
using System.Threading.Tasks;

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
