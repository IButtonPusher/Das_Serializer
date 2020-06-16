using System;
using System.Threading.Tasks;

namespace Das.Serializer.Objects
{
    public interface IStronglyTyped
    {
        Type Type { get; set; }
    }
}