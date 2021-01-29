using System;
using System.Threading.Tasks;

namespace Das.Serializer
{
    public enum DefaultValueOmission
    {
        OmitNothing,
        OmitNullReferenceTypes,
        OmitDefaultValueTypes,
        OmitAllDefaultValues
    }
}
