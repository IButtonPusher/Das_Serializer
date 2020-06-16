using System;
using System.Threading.Tasks;
using Das.Serializer.Objects;

namespace Das.Serializer
{
    public interface INamedField : IStronglyTyped
    {
        String Name { get; }
    }
}