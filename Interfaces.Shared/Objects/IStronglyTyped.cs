using System;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    public interface IStronglyTyped
    {
        //if this is nullable := a lot of warnings
        Type Type { get; set; }
    }
}